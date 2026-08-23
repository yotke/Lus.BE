using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lus.Application.Common.Options;
using Lus.Application.Common.Ports;
using Lus.Contracts.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lus.Application.Common.Builders
{
    /// <summary>
    /// Entity-agnostic subprocess-call core for AI builder agents, over
    /// <see cref="IPythonScriptsAdapter.RunAgentAsync"/> (PythonScripts/agents/runner.py).
    /// Extracted bottom-up (ARCH-1) from Organizations/Builder/Agents/BuilderAgentClient once
    /// the Rules builder needed the identical timeout/failure-mapping machinery. Every failure
    /// mode DEGRADES the call instead of throwing: timeouts, crashes, bad output and Ok:false
    /// envelopes all map to <see cref="AgentResult{T}"/>.Failed — the caller decides how to
    /// surface that (skip a question, add a warning note, etc).
    ///
    /// Generic over the draft: the draft is serialized by its RUNTIME type
    /// (<c>JsonSerializer.Serialize(object, Type, options)</c>), so a caller passing a
    /// compile-time-typed draft gets byte-identical JSON to a hand-rolled
    /// <c>JsonSerializer.Serialize&lt;TDraft&gt;</c> call — no generic type parameter needed on
    /// this class itself.
    /// </summary>
    public class BuilderAgentClientCore
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly IPythonScriptsAdapter python;
        private readonly IOptions<AiBuilderOptions> options;
        private readonly ILogger logger;

        public BuilderAgentClientCore(
            IPythonScriptsAdapter python,
            IOptions<AiBuilderOptions> options,
            ILogger logger)
        {
            this.python = python;
            this.options = options;
            this.logger = logger;
        }

        public async Task<AgentResult<T>> RunAsync<T>(
            string agentName, object draft, LanguageType language, CancellationToken ct,
            string inputJson = "null", bool keepStringNotes = false)
            where T : class
        {
            var langCode = language.ToLangCode();

            string raw;
            var started = Stopwatch.StartNew();
            try
            {
                // Per-agent budget inside the turn budget.
                using var agentCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                agentCts.CancelAfter(TimeSpan.FromSeconds(this.options.Value.AgentTimeoutSeconds));

                var draftJson = JsonSerializer.Serialize(draft, draft.GetType(), Json);
                raw = await this.python.RunAgentAsync(agentName, draftJson, inputJson, langCode, agentCts.Token);

                // THE TURN'S ONLY PER-AGENT CLOCK. Without it a slow turn is one opaque block:
                // job d61fb1b5 (2026-08-11) ran 149 seconds between its first and second log line
                // with nothing to say which of router/classifier/wave-1/wave-2/rules_text owned the
                // time. Emitted on success AND on every failure path below — a slow FAILURE is
                // exactly the case worth seeing. Payload size rides along because prompt length is
                // the first thing to suspect when one agent dominates a turn.
                this.logger.LogInformation(
                    "Agent {Agent} finished in {ElapsedMs}ms (draft {DraftBytes}B, input {InputBytes}B).",
                    agentName, started.ElapsedMilliseconds, draftJson.Length, inputJson?.Length ?? 0);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                this.logger.LogWarning("Agent {Agent} timed out after {Seconds}s (ran {ElapsedMs}ms).",
                    agentName, this.options.Value.AgentTimeoutSeconds, started.ElapsedMilliseconds);
                return AgentResult<T>.Failed("agent_timeout", AiUserMessages.TimeoutError(language));
            }
            catch (OperationCanceledException)
            {
                throw; // the turn itself was cancelled — let the job wrapper handle it
            }
            catch (Exception ex) when (ct.IsCancellationRequested)
            {
                // CANCELLATION MUST NOT MASQUERADE AS A CRASH.
                // PythonScriptsAdapter.RunAgentAsync kills the child from a token registration, so
                // whatever I/O is in flight at that instant fails with whatever the OS raises — not
                // with OperationCanceledException. On job d61fb1b5 the stdin write lost its reader
                // and threw IOException("Broken pipe"), which fell into the generic catch below and
                // logged "[ERR] Agent validator_agent failed to run." The validator never ran: the
                // turn budget had expired 14 seconds earlier. Blaming the agent for a dead turn's
                // collateral sends the next reader hunting a python bug that does not exist.
                this.logger.LogInformation(
                    "Agent {Agent} aborted after {ElapsedMs}ms — the turn was already cancelled ({ExType}).",
                    agentName, started.ElapsedMilliseconds, ex.GetType().Name);
                throw new OperationCanceledException(
                    $"Turn cancelled during agent '{agentName}'.", ex, ct);
            }
            catch (Exception ex)
            {
                // PythonScriptException (crash / missing runner) and anything unexpected:
                // logged loudly, surfaced softly.
                this.logger.LogError(ex, "Agent {Agent} failed to run (after {ElapsedMs}ms).",
                    agentName, started.ElapsedMilliseconds);
                return AgentResult<T>.Failed("agent_crashed", AiUserMessages.UnexpectedError(language));
            }

            AgentEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<AgentEnvelope>(raw, Json);
            }
            catch (JsonException ex)
            {
                this.logger.LogError(ex, "Agent {Agent} returned unparsable output: {Head}",
                    agentName, raw.Length > 300 ? raw[..300] : raw);
                return AgentResult<T>.Failed("agent_bad_output", AiUserMessages.UnexpectedError(language));
            }

            if (envelope is null)
                return AgentResult<T>.Failed("agent_bad_output", AiUserMessages.UnexpectedError(language));

            if (!envelope.Ok)
            {
                var code = envelope.ErrorInfo?.Code ?? "agent_error";
                var message = language == LanguageType.He
                    ? envelope.ErrorInfo?.UserMessage
                    : envelope.ErrorInfo?.UserMessageEn;
                this.logger.LogWarning("Agent {Agent} reported failure {Code}.", agentName, code);
                return AgentResult<T>.Failed(code, message ?? AiUserMessages.UnexpectedError(language));
            }

            if (envelope.Result is null)
                return AgentResult<T>.Failed("agent_bad_output", AiUserMessages.UnexpectedError(language));

            try
            {
                // A single malformed advisory note (e.g. a bare string instead of a
                // {Code,Message} object) must NOT discard the whole agent result — the
                // patches/warnings are what matter. Drop the bad note entries, keep the rest.
                var sanitized = SanitizeNoteArrays(envelope.Result.Value, agentName, keepStringNotes);
                var value = sanitized.Deserialize<T>(Json);
                return value is null
                    ? AgentResult<T>.Failed("agent_bad_output", AiUserMessages.UnexpectedError(language))
                    : AgentResult<T>.Success(value);
            }
            catch (JsonException ex)
            {
                this.logger.LogError(ex, "Agent {Agent} result does not match {Type}.", agentName, typeof(T).Name);
                return AgentResult<T>.Failed("agent_bad_output", AiUserMessages.UnexpectedError(language));
            }
        }

        // Top-level result properties that carry advisory notes / warnings
        // (List&lt;DraftWarningDto&gt;) across every agent result shape.
        private static readonly HashSet<string> NoteArrayProps =
            new(StringComparer.OrdinalIgnoreCase) { "Notes", "Warnings" };

        /// <summary>
        /// Returns the agent result JSON with any Notes/Warnings array pruned of entries
        /// that are not objects (a bare string can't map to a warning DTO and would otherwise
        /// throw and sink the entire result). Every dropped entry is logged. Non-object results
        /// and well-formed notes pass through untouched.
        /// <paramref name="keepStringNotes"/> (generate-lane agents): those agents' Notes are
        /// STRING sentinels BY CONTRACT (e.g. "dropped_invalid_ops:3", "llm_unavailable") mapped
        /// to List&lt;string&gt; — string entries are kept, only non-string non-objects pruned.
        /// Default false ⇒ byte-identical behavior for every existing caller.
        /// </summary>
        private JsonNode SanitizeNoteArrays(JsonElement result, string agentName, bool keepStringNotes = false)
        {
            var node = JsonNode.Parse(result.GetRawText())!;
            if (node is not JsonObject obj)
                return node;

            foreach (var (key, child) in obj.ToList())
            {
                if (!NoteArrayProps.Contains(key) || child is not JsonArray arr)
                    continue;

                for (var i = arr.Count - 1; i >= 0; i--)
                {
                    if (arr[i] is JsonObject)
                        continue;
                    if (keepStringNotes && arr[i] is JsonValue v && v.TryGetValue<string>(out _))
                        continue;
                    this.logger.LogWarning(
                        "Agent {Agent} emitted a malformed '{Prop}' note entry — skipping it.",
                        agentName, key);
                    arr.RemoveAt(i);
                }
            }

            return node;
        }

        private sealed class AgentEnvelope
        {
            public bool Ok { get; set; }
            public string? Agent { get; set; }
            public int SchemaVersion { get; set; }
            public JsonElement? Result { get; set; }
            public AgentErrorInfo? ErrorInfo { get; set; }
        }

        private sealed class AgentErrorInfo
        {
            public string? Code { get; set; }
            public string? UserMessage { get; set; }
            public string? UserMessageEn { get; set; }
        }
    }
}
