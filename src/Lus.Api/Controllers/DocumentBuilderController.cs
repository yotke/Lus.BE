using System.Text.Json;
using Lus.Application.Common.Ports;
using Lus.Application.Documents.Builder.Commands.CanvasEdit;
using Lus.Application.Documents.Builder.Commands.ImportTemplate;
using Lus.Application.Documents.Builder.Commands.Redo;
using Lus.Application.Documents.Builder.Commands.RunTurn;
using Lus.Application.Documents.Builder.Commands.Undo;
using Lus.Application.Documents.Builder.Queries.GetSession;
using Lus.Application.Documents.Builder.Services;
using Lus.Authorization.Authentication;
using Lus.Application.Common.Builders;
using Lus.Contracts.Common;
using Lus.Contracts.Common.Builders;
using Lus.Contracts.Documents.Builder;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lus.Controllers
{
    [ApiController]
    [Route("v1/documents/builder")]
    [Authorize(AuthenticationSchemes = CookieAuthSchemes.Api)]
    public class DocumentBuilderController : ControllerBase
    {
        /// <summary>
        /// PascalCase on purpose. The hub is configured with `PropertyNamingPolicy = null`
        /// (Startup.cs), so builder events arrive PascalCase; if these endpoints answered in
        /// camelCase the client would need two shapes for the same patch op depending on which
        /// transport delivered it. One casing, one contract.
        /// </summary>
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = null,
        };
        /// <summary>Agent name as the runner's alias table spells it.</summary>
        private const string EchoAgentName = "doc.echo";

        /// <summary>Exemplar workbooks are read by openpyxl, which only accepts these.</summary>
        private static readonly string[] AllowedUploadExtensions = { ".xlsx", ".xlsm" };

        /// <summary>An exemplar is a report template, not a data dump — 10 MB is generous.</summary>
        private const long MaxUploadBytes = 10 * 1024 * 1024;

        private readonly IPythonScriptsAdapter python;
        private readonly IMediator mediator;
        private readonly IBuilderAgentCatalog catalog;
        private readonly ILogger<DocumentBuilderController> logger;

        public DocumentBuilderController(
            IPythonScriptsAdapter python,
            IMediator mediator,
            IBuilderAgentCatalog catalog,
            ILogger<DocumentBuilderController> logger)
        {
            this.python = python;
            this.mediator = mediator;
            this.catalog = catalog;
            this.logger = logger;
        }

        /// <summary>
        /// The agent catalog as the UI renders it. Served from the same C# catalog the
        /// orchestrator dispatches from, so the client's pipeline cannot drift out of sync.
        /// </summary>
        [HttpGet("agents")]
        [ProducesResponseType(typeof(IReadOnlyList<DocumentAgentDto>), StatusCodes.Status200OK)]
        public ActionResult<IReadOnlyList<DocumentAgentDto>> Agents()
        {
            var dtos = this.catalog.All.Select(d => new DocumentAgentDto
            {
                Name = d.Name,
                Kind = d.Kind.ToString(),
                InputKind = d.InputKind.ToString(),
                Icon = d.Icon,
                DisplayNameKey = d.DisplayNameKey,
                DescriptionKey = d.DescriptionKey,
                Enabled = d.Enabled,
                Wave = d is ContentAgentDescriptor c ? c.Wave : null,
            }).ToList();

            return JsonContent(dtos);
        }

        /// <summary>
        /// A hand edit on the canvas. Deliberately the same patch-op shape an agent emits —
        /// one write path, so undo/redo and the version guard behave identically for both.
        /// A stale version returns 409 with the current version so the client can re-base.
        /// </summary>
        [HttpPost("canvas")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(DocumentBuilderTurnResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Canvas([FromBody] CanvasEditRequestDto body, CancellationToken ct)
        {
            if (body.Ops.Count == 0)
                return BadRequest(new { Error = "No ops supplied." });

            try
            {
                var dto = await this.mediator.Send(
                    new ApplyCanvasEditCommand { Version = body.Version, Ops = body.Ops }, ct);
                return JsonContent(dto);
            }
            catch (DraftVersionConflictException ex)
            {
                return Conflict(new { ex.Expected, ex.Actual });
            }
            catch (InvalidOperationException ex)
            {
                // Derived cells (totals) rejected by the orchestrator's guard — but this also
                // catches anything else that throws InvalidOperationException deeper in the
                // stack, where the framework's default message says nothing useful. Log the
                // real exception so a report of "400, operation is not valid" is diagnosable.
                this.logger.LogError(ex, "Canvas edit failed. Ops: {Ops}",
                    string.Join(", ", body.Ops.Select(o => $"{o.Op} {o.Path}")));
                return BadRequest(new { Error = ex.Message, Type = ex.GetType().Name });
            }
        }

        /// <summary>
        /// Upload an exemplar workbook so the Importer can learn its structure. The file is
        /// staged on disk because the Python agent opens it with openpyxl by path — the bytes
        /// never travel as JSON.
        /// </summary>
        [HttpPost("upload")]
        [Produces("application/json")]
        [RequestSizeLimit(MaxUploadBytes)]
        [ProducesResponseType(typeof(TemplateImportResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { Error = "No file supplied." });

            if (file.Length > MaxUploadBytes)
                return BadRequest(new { Error = "File is too large." });

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedUploadExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { Error = $"Unsupported file type '{extension}'." });

            // Random name, never the client's: an uploaded filename is untrusted input and
            // must not be able to steer where we write.
            var staged = Path.Combine(Path.GetTempPath(), $"lus-exemplar-{Guid.NewGuid():N}{extension}");
            try
            {
                await using (var stream = System.IO.File.Create(staged))
                {
                    await file.CopyToAsync(stream, ct);
                }

                var dto = await this.mediator.Send(new ImportTemplateCommand { FilePath = staged }, ct);
                return JsonContent(dto);
            }
            finally
            {
                // The importer has read what it needs; the exemplar itself is not ours to keep.
                try { if (System.IO.File.Exists(staged)) System.IO.File.Delete(staged); }
                catch { /* a leftover temp file must never fail the request */ }
            }
        }

        /// <summary>
        /// Bridge smoke check: round-trips <paramref name="body"/>.Text through the real Python
        /// subprocess. Proves the interpreter, the scripts path and the UTF-8 journey are healthy
        /// in the running container — failures that stay invisible until an agent is invoked.
        /// </summary>
        [HttpPost("echo")]
        [ProducesResponseType(typeof(AgentEnvelopeDto<EchoResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AgentEnvelopeDto<EchoResultDto>>> Echo(
            [FromBody] EchoRequestDto body, CancellationToken ct)
        {
            // Typed input, not an anonymous object: the property name is the wire contract the
            // Python agent reads ("Text"), so it belongs in a DTO the compiler can check.
            var input = new EchoAgentInputDto { Text = body.Text };
            var inputJson = JsonSerializer.Serialize(input, AgentEnvelopeParser.Options);

            var raw = await this.python.RunAgentAsync(
                EchoAgentName, "{}", inputJson, LanguageType.He.ToLangCode(), ct);

            // Deserialized, not forwarded: the endpoint returns a real contract so Swagger and
            // every client get a typed shape instead of an opaque string.
            return Ok(AgentEnvelopeParser.Parse<EchoResultDto>(raw, EchoAgentName));
        }

        [HttpGet("session")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(DocumentBuilderSessionDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Session(CancellationToken ct)
        {
            var dto = await this.mediator.Send(new GetDocumentBuilderSessionQuery(), ct);
            return JsonContent(dto);
        }

        [HttpPost("turn")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(DocumentBuilderTurnResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Turn([FromBody] DocumentBuilderTurnRequestDto body, CancellationToken ct)
        {
            try
            {
                var dto = await this.mediator.Send(
                    new RunDocumentBuilderTurnCommand
                    {
                        Version = body.Version,
                        Text = body.Text,
                        QuestionId = body.QuestionId,
                    }, ct);
                return JsonContent(dto);
            }
            catch (DraftVersionConflictException ex)
            {
                return Conflict(new { ex.Expected, ex.Actual });
            }
        }

        [HttpPost("undo")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(DocumentBuilderTurnResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Undo(CancellationToken ct)
        {
            var dto = await this.mediator.Send(new UndoDocumentBuilderCommand(), ct);
            return JsonContent(dto);
        }

        [HttpPost("redo")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(DocumentBuilderTurnResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Redo(CancellationToken ct)
        {
            var dto = await this.mediator.Send(new RedoDocumentBuilderCommand(), ct);
            return JsonContent(dto);
        }

        private ContentResult JsonContent<T>(T value) =>
            Content(JsonSerializer.Serialize(value, Json), "application/json");
    }
}
