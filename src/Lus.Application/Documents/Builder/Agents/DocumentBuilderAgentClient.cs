using System.Text.Json;
using Lus.Application.Common.Builders;
using Lus.Application.Common.Options;
using Lus.Application.Common.Ports;
using Lus.Contracts.Common;
using Lus.Contracts.Documents.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lus.Application.Documents.Builder.Agents
{
    /// <summary>
    /// Thin wrapper over <see cref="BuilderAgentClientCore"/>. Maps each catalog agent
    /// onto patch ops returned from Python.
    /// </summary>
    public class DocumentBuilderAgentClient
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private readonly BuilderAgentClientCore core;

        public DocumentBuilderAgentClient(
            IPythonScriptsAdapter python,
            IOptions<AiBuilderOptions> options,
            ILogger<DocumentBuilderAgentClient> logger)
        {
            this.core = core = new BuilderAgentClientCore(python, options, logger);
        }

        public async Task<WaveAgentOutcome<DraftPatchOp>> RunAsync(
            BuilderAgentDescriptor descriptor,
            object draft,
            string inputJson,
            LanguageType language,
            CancellationToken ct)
        {
            if (descriptor.Name == "doc.echo")
                return await RunEchoAsync(draft, inputJson, language, ct);

            if (descriptor is ContentAgentDescriptor { ProducesPatches: true }
                or ImporterAgentDescriptor { ProducesPatches: true }
                or ValidatorAgentDescriptor { ProducesPatches: true })
            {
                var patchResult = await this.core.RunAsync<AgentPatchesResult>(
                    descriptor.Name, draft, language, ct, inputJson);
                if (!patchResult.Ok)
                    return WaveAgentOutcome<DraftPatchOp>.Failed(patchResult.FailureCode, patchResult.FailureMessage);
                return WaveAgentOutcome<DraftPatchOp>.Success(patchResult.Value?.Patches ?? new List<DraftPatchOp>());
            }

            // Read-only / advisory agents: success with no patches.
            var noop = await this.core.RunAsync<AgentNotesResult>(
                descriptor.Name, draft, language, ct, inputJson);
            if (!noop.Ok)
                return WaveAgentOutcome<DraftPatchOp>.Failed(noop.FailureCode, noop.FailureMessage);
            return WaveAgentOutcome<DraftPatchOp>.Success(Array.Empty<DraftPatchOp>());
        }

        private async Task<WaveAgentOutcome<DraftPatchOp>> RunEchoAsync(
            object draft, string inputJson, LanguageType language, CancellationToken ct)
        {
            var echo = await this.core.RunAsync<EchoResultDto>(
                "doc.echo", draft, language, ct, inputJson);
            if (!echo.Ok)
                return WaveAgentOutcome<DraftPatchOp>.Failed(echo.FailureCode, echo.FailureMessage);

            var uttered = echo.Value?.Echo ?? "";
            return WaveAgentOutcome<DraftPatchOp>.Success(new[]
            {
                new DraftPatchOp
                {
                    Op = "SetField",
                    Path = "lastUtterance",
                    Value = JsonSerializer.SerializeToElement(uttered, Json)
                }
            });
        }

        /// <summary>
        /// Planner run: returns the single next question, or null when the planner decides
        /// it has nothing worth asking. A planner failure is never fatal to a turn — the
        /// document is still correct without a follow-up question.
        /// </summary>
        public async Task<DocumentQuestionDto?> RunPlannerAsync(
            BuilderAgentDescriptor descriptor,
            object draft,
            string inputJson,
            LanguageType language,
            CancellationToken ct)
        {
            var result = await this.core.RunAsync<AgentQuestionResult>(
                descriptor.Name, draft, language, ct, inputJson);
            return result.Ok ? result.Value?.Question : null;
        }

        /// <summary>Advisor run: grounded free-text answer plus growth chips.</summary>
        public async Task<AgentAdviceResult?> RunAdvisorAsync(
            BuilderAgentDescriptor descriptor,
            object draft,
            string inputJson,
            LanguageType language,
            CancellationToken ct)
        {
            var result = await this.core.RunAsync<AgentAdviceResult>(
                descriptor.Name, draft, language, ct, inputJson);
            return result.Ok ? result.Value : null;
        }

        /// <summary>
        /// Validator run: warnings AND auto-fix patches. The generic <see cref="RunAsync"/>
        /// path returns the patches but drops the warnings on the floor; the chat rail needs
        /// both, so the validator gets its own typed call.
        /// </summary>
        public async Task<AgentValidationResult?> RunValidatorAsync(
            BuilderAgentDescriptor descriptor,
            object draft,
            string inputJson,
            LanguageType language,
            CancellationToken ct)
        {
            var result = await this.core.RunAsync<AgentValidationResult>(
                descriptor.Name, draft, language, ct, inputJson);
            return result.Ok ? result.Value : null;
        }

        public sealed class AgentQuestionResult
        {
            public DocumentQuestionDto? Question { get; set; }
        }

        public sealed class AgentAdviceResult
        {
            public string Answer { get; set; } = "";
            public List<string> Suggestions { get; set; } = new();
        }

        public sealed class AgentValidationResult
        {
            public bool Ok { get; set; }
            public List<DocumentWarningDto> Warnings { get; set; } = new();
            public List<DraftPatchOp> Patches { get; set; } = new();
        }

        private sealed class AgentPatchesResult
        {
            public List<DraftPatchOp> Patches { get; set; } = new();
        }

        private sealed class AgentNotesResult
        {
            public List<string> Notes { get; set; } = new();
        }
    }
}
