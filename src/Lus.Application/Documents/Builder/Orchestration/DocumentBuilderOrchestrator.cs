using Lus.Application.Common.Builders;
using Lus.Application.Documents.Builder.Agents;
using Lus.Application.Documents.Builder.Services;
using Lus.Contracts.Common;
using Lus.Contracts.Documents.Builder;
using Microsoft.Extensions.Logging;

namespace Lus.Application.Documents.Builder.Orchestration
{
    public sealed class DocumentBuilderTurnResult
    {
        public int Version { get; init; }
        public IReadOnlyList<DraftPatchOp> Ops { get; init; } = Array.Empty<DraftPatchOp>();
        public DocumentDraftDto Draft { get; init; } = new();

        /// <summary>The planner's single next question, when it asked one.</summary>
        public DocumentQuestionDto? Question { get; init; }

        /// <summary>Assistant chat lines produced during the turn.</summary>
        public IReadOnlyList<DocumentBuilderMessageDto> Messages { get; init; } = Array.Empty<DocumentBuilderMessageDto>();

        /// <summary>Validator findings for this turn.</summary>
        public IReadOnlyList<DocumentWarningDto> Warnings { get; init; } = Array.Empty<DocumentWarningDto>();
    }

    public class DocumentBuilderOrchestrator
    {
        private readonly DocumentBuildSessionStore sessions;
        private readonly IBuilderAgentCatalog catalog;
        private readonly DocumentBuilderAgentClient agents;
        private readonly IDocumentBuilderEventSender events;
        private readonly ILogger<DocumentBuilderOrchestrator> logger;

        public DocumentBuilderOrchestrator(
            DocumentBuildSessionStore sessions,
            IBuilderAgentCatalog catalog,
            DocumentBuilderAgentClient agents,
            IDocumentBuilderEventSender events,
            ILogger<DocumentBuilderOrchestrator> logger)
        {
            this.sessions = sessions;
            this.catalog = catalog;
            this.agents = agents;
            this.events = events;
            this.logger = logger;
        }

        public async Task<DocumentBuilderTurnResult> GetSessionAsync(int userId, CancellationToken ct)
        {
            var session = await this.sessions.GetOrCreateAsync(userId, ct);
            return new DocumentBuilderTurnResult { Version = session.Draft.Version, Draft = session.Draft };
        }

        /// <summary>Back-compat overload: a turn that answers no question.</summary>
        public Task<DocumentBuilderTurnResult> RunTurnAsync(
            int userId, int version, string? text, CancellationToken ct) =>
            RunTurnAsync(userId, version, text, questionId: null, ct);

        public async Task<DocumentBuilderTurnResult> RunTurnAsync(
            int userId, int version, string? text, string? questionId, CancellationToken ct)
        {
            var session = await this.sessions.GetOrCreateAsync(userId, ct);
            if (session.Draft.Version != version)
                throw new DraftVersionConflictException(version, session.Draft.Version);

            var jobId = $"doc-{userId}";
            var userKey = userId.ToString();
            var inputJson = System.Text.Json.JsonSerializer.Serialize(new { Text = text ?? "" });
            var collected = new List<DraftPatchOp>();

            // An answer to a field question is that field's value, not a new work item.
            // Handing "225" to the content wave would extract a nonsense row and leave the
            // rate empty — which is exactly the loop the user hit: answer, re-asked, answer.
            var bound = QuestionAnswerBinder.Bind(questionId, text);
            if (bound is not null)
            {
                collected.AddRange(PatchAuthorStamp.Stamp(new[] { bound }, PatchAuthorStamp.User));
                return await FinishTurnAsync(session, version, jobId, userKey, collected, inputJson, ct);
            }

            await SequentialAgentWaveRunner.RunAsync(
                this.catalog.Content,
                runAgentAsync: (descriptor, token) =>
                    this.agents.RunAsync(descriptor, session.Draft, inputJson, LanguageType.He, token),
                applyAndSaveAsync: (descriptor, patches, _) =>
                {
                    // Stamped here, not at Apply time: only this callback knows WHICH agent
                    // produced these patches.
                    collected.AddRange(PatchAuthorStamp.Stamp(patches, descriptor.Name));
                    return Task.CompletedTask;
                },
                sendStatusAsync: async (descriptor, state, detail, token) =>
                {
                    try
                    {
                        await this.events.SendAgentStatusAsync(jobId, userKey, descriptor.Name, state, detail, token);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex, "AgentStatus SignalR send failed.");
                    }
                },
                addFailureNote: (descriptor, outcome) =>
                    this.logger.LogWarning(
                        "Agent {Agent} degraded: {Code} {Message}",
                        descriptor.Name, outcome.FailureCode, outcome.FailureMessage),
                ct);

            return await FinishTurnAsync(session, version, jobId, userKey, collected, inputJson, ct);
        }

        /// <summary>
        /// The half of a turn that is the same however the patches were produced: validate,
        /// derive totals, commit under the version guard, then ask the next question.
        /// </summary>
        private async Task<DocumentBuilderTurnResult> FinishTurnAsync(
            DocumentBuildSession session,
            int version,
            string jobId,
            string userKey,
            List<DraftPatchOp> collected,
            string inputJson,
            CancellationToken ct)
        {
            // The validator runs against the draft AS THE CONTENT WAVE LEFT IT, so its
            // auto-fixes land in the same versioned batch rather than a second one.
            var previewForChecks = collected.Count > 0
                ? DraftPatcher.Preview(session.Draft, collected)
                : session.Draft;

            var warnings = new List<DocumentWarningDto>();
            var validator = this.catalog.All.FirstOrDefault(
                d => d is ValidatorAgentDescriptor { Enabled: true });
            if (validator is not null)
            {
                var outcome = await RunSafelyAsync(
                    jobId, userKey, validator,
                    token => this.agents.RunValidatorAsync(validator, previewForChecks, inputJson, LanguageType.He, token),
                    ct);
                if (outcome is not null)
                {
                    warnings.AddRange(outcome.Warnings);
                    collected.AddRange(outcome.Patches);
                }
            }

            if (collected.Count > 0)
            {
                var preview = DraftPatcher.Preview(session.Draft, collected);
                var totals = DocumentTotalsCalculator.Diff(preview);
                if (totals is not null)
                    collected.Add(totals);
            }

            // Question and advice are computed against the post-patch document, so the
            // planner never asks about something this turn just filled in.
            var afterPatches = collected.Count > 0
                ? DraftPatcher.Preview(session.Draft, collected)
                : session.Draft;

            var question = await RunPlannerAsync(jobId, userKey, afterPatches, inputJson, ct);
            var messages = await RunAdvisorAsync(jobId, userKey, afterPatches, inputJson, ct);

            if (collected.Count == 0)
                return new DocumentBuilderTurnResult
                {
                    Version = session.Draft.Version,
                    Ops = Array.Empty<DraftPatchOp>(),
                    Draft = session.Draft,
                    Question = question,
                    Messages = messages,
                    Warnings = warnings,
                };

            var (next, inverse) = DraftPatcher.Apply(session.Draft, version, collected);
            session.Draft = next;
            session.UndoForwards.Add(collected);
            session.UndoInverses.Add(inverse.ToList());
            session.RedoForwards.Clear();
            session.RedoInverses.Clear();
            await this.sessions.SaveAsync(session, ct);

            try
            {
                await this.events.SendDraftPatchedAsync(
                    jobId, userKey, sessionId: userKey, next.Version, collected, ct);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "DraftPatched SignalR send failed — turn still committed.");
            }

            return new DocumentBuilderTurnResult
            {
                Version = next.Version,
                Ops = collected,
                Draft = next,
                Question = question,
                Messages = messages,
                Warnings = warnings,
            };
        }

        /// <summary>
        /// A human edit from the canvas — routed through the SAME patcher, version guard and
        /// undo stack an agent's output goes through. There is deliberately no second write
        /// path: one op shape, one history, one conflict rule.
        /// </summary>
        public async Task<DocumentBuilderTurnResult> ApplyCanvasEditAsync(
            int userId, int version, IReadOnlyList<DraftPatchOp> ops, CancellationToken ct)
        {
            var session = await this.sessions.GetOrCreateAsync(userId, ct);
            if (session.Draft.Version != version)
                throw new DraftVersionConflictException(version, session.Draft.Version);

            foreach (var op in ops)
                GuardEditablePath(op);

            var batch = PatchAuthorStamp.Stamp(ops, PatchAuthorStamp.User);
            // Totals are derived, never typed: recompute them from the edited rows so a hand
            // edit and an agent edit leave the document in the same state.
            var preview = DraftPatcher.Preview(session.Draft, batch);
            var totals = DocumentTotalsCalculator.Diff(preview);
            if (totals is not null)
                batch.Add(totals);

            var (next, inverse) = DraftPatcher.Apply(session.Draft, version, batch);
            session.Draft = next;
            session.UndoForwards.Add(batch);
            session.UndoInverses.Add(inverse.ToList());
            session.RedoForwards.Clear();
            session.RedoInverses.Clear();
            await this.sessions.SaveAsync(session, ct);

            var userKey = userId.ToString();
            try
            {
                await this.events.SendDraftPatchedAsync(
                    $"doc-{userId}", userKey, sessionId: userKey, next.Version, batch, ct);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "DraftPatched SignalR send failed — canvas edit still committed.");
            }

            return new DocumentBuilderTurnResult { Version = next.Version, Ops = batch, Draft = next };
        }

        /// <summary>
        /// Learn an uploaded exemplar workbook: the Importer agent reads it with openpyxl and
        /// returns patches describing what it found. The file path is what crosses the Python
        /// bridge — the workbook itself never travels as JSON.
        /// </summary>
        public async Task<DocumentBuilderTurnResult> ImportTemplateAsync(
            int userId, string filePath, CancellationToken ct)
        {
            var session = await this.sessions.GetOrCreateAsync(userId, ct);
            var importer = this.catalog.All.FirstOrDefault(
                d => d is ImporterAgentDescriptor { Enabled: true });
            if (importer is null)
                return new DocumentBuilderTurnResult { Version = session.Draft.Version, Draft = session.Draft };

            var jobId = $"doc-{userId}";
            var userKey = userId.ToString();
            var inputJson = System.Text.Json.JsonSerializer.Serialize(new { FilePath = filePath });

            await SendStatusSafelyAsync(jobId, userKey, importer.Name, "running", null, ct);
            var outcome = await this.agents.RunAsync(
                importer, session.Draft, inputJson, LanguageType.He, ct);

            if (!outcome.Ok)
            {
                await SendStatusSafelyAsync(jobId, userKey, importer.Name, "failed", outcome.FailureMessage, ct);
                try
                {
                    await this.events.SendErrorAsync(
                        jobId, userKey, outcome.FailureCode ?? "ImportFailed", outcome.FailureMessage ?? "", ct);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "BuilderError SignalR send failed.");
                }
                return new DocumentBuilderTurnResult { Version = session.Draft.Version, Draft = session.Draft };
            }

            await SendStatusSafelyAsync(jobId, userKey, importer.Name, "done", null, ct);

            var patches = PatchAuthorStamp.Stamp(
                outcome.Patches ?? Array.Empty<DraftPatchOp>(), importer.Name);
            if (patches.Count == 0)
                return new DocumentBuilderTurnResult { Version = session.Draft.Version, Draft = session.Draft };

            var (next, inverse) = DraftPatcher.Apply(session.Draft, session.Draft.Version, patches);
            session.Draft = next;
            session.UndoForwards.Add(patches);
            session.UndoInverses.Add(inverse.ToList());
            session.RedoForwards.Clear();
            session.RedoInverses.Clear();
            await this.sessions.SaveAsync(session, ct);

            try
            {
                await this.events.SendDraftPatchedAsync(
                    jobId, userKey, sessionId: userKey, next.Version, patches, ct);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "DraftPatched SignalR send failed — import still committed.");
            }

            // Learning the shape is only half the job: the document is now a known-empty form,
            // so the planner should immediately start asking for what it needs rather than
            // leaving the user staring at a blank grid wondering what to type.
            var importInput = System.Text.Json.JsonSerializer.Serialize(new { Text = "" });
            var question = await RunPlannerAsync(jobId, userKey, next, importInput, ct);
            var messages = await RunAdvisorAsync(jobId, userKey, next, importInput, ct);

            return new DocumentBuilderTurnResult
            {
                Version = next.Version,
                Ops = patches,
                Draft = next,
                Question = question,
                Messages = messages,
            };
        }

        /// <summary>
        /// The billing block holds INPUTS and DERIVED values side by side, and telling them
        /// apart is the whole point.
        ///
        /// Inputs (rate, carry-in, VAT %, plots %) must be editable — the shipped exemplar
        /// invoices bill 0.00 for real work precisely because the rate cell was left empty and
        /// nothing let anyone fill it. Derived cells (hours, remaining, total) stay read-only:
        /// typing over a computed number is how a document ends up disagreeing with its own
        /// arithmetic (smart concept C3).
        /// </summary>
        private static readonly HashSet<string> EditableTotalsPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "totals.hourlyRate",
            "totals.carryIn",
            "totals.vatPercent",
            "totals.plotsPercent",
        };

        private static void GuardEditablePath(DraftPatchOp op)
        {
            if (!op.Path.StartsWith("totals.", StringComparison.OrdinalIgnoreCase))
                return;

            if (EditableTotalsPaths.Contains(op.Path))
                return;

            throw new InvalidOperationException(
                $"Path '{op.Path}' is derived and cannot be edited directly. Edit the inputs instead.");
        }

        private async Task<DocumentQuestionDto?> RunPlannerAsync(
            string jobId, string userKey, DocumentDraftDto draft, string inputJson, CancellationToken ct)
        {
            var planner = this.catalog.All.FirstOrDefault(d => d is PlannerAgentDescriptor { Enabled: true });
            if (planner is null) return null;

            var question = await RunSafelyAsync(
                jobId, userKey, planner,
                token => this.agents.RunPlannerAsync(planner, draft, inputJson, LanguageType.He, token),
                ct);
            if (question is null) return null;

            try
            {
                await this.events.SendQuestionAskedAsync(jobId, userKey, question, ct);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "QuestionAsked SignalR send failed.");
            }
            return question;
        }

        private async Task<IReadOnlyList<DocumentBuilderMessageDto>> RunAdvisorAsync(
            string jobId, string userKey, DocumentDraftDto draft, string inputJson, CancellationToken ct)
        {
            var advisor = this.catalog.All.FirstOrDefault(d => d is AdvisorAgentDescriptor { Enabled: true });
            if (advisor is null) return Array.Empty<DocumentBuilderMessageDto>();

            var advice = await RunSafelyAsync(
                jobId, userKey, advisor,
                token => this.agents.RunAdvisorAsync(advisor, draft, inputJson, LanguageType.He, token),
                ct);
            if (advice is null || string.IsNullOrWhiteSpace(advice.Answer))
                return Array.Empty<DocumentBuilderMessageDto>();

            var message = new DocumentBuilderMessageDto
            {
                Role = "assistant",
                Text = advice.Answer,
                Suggestions = advice.Suggestions ?? new List<string>(),
            };

            try
            {
                await this.events.SendBuilderMessageAsync(
                    jobId, userKey, message.Role, message.Text, ct, message.Suggestions);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "BuilderMessage SignalR send failed.");
            }
            return new[] { message };
        }

        /// <summary>
        /// Runs one non-content agent with its status narrated and its failures swallowed.
        /// A planner or advisor that dies must never cost the user the document the content
        /// wave just produced.
        /// </summary>
        private async Task<T?> RunSafelyAsync<T>(
            string jobId,
            string userKey,
            BuilderAgentDescriptor descriptor,
            Func<CancellationToken, Task<T?>> run,
            CancellationToken ct) where T : class
        {
            await SendStatusSafelyAsync(jobId, userKey, descriptor.Name, "running", null, ct);
            try
            {
                var value = await run(ct);
                await SendStatusSafelyAsync(jobId, userKey, descriptor.Name, "done", null, ct);
                return value;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Agent {Agent} failed; turn continues.", descriptor.Name);
                await SendStatusSafelyAsync(jobId, userKey, descriptor.Name, "failed", ex.Message, ct);
                return null;
            }
        }

        private async Task SendStatusSafelyAsync(
            string jobId, string userKey, string agent, string state, string? detail, CancellationToken ct)
        {
            try
            {
                await this.events.SendAgentStatusAsync(jobId, userKey, agent, state, detail, ct);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "AgentStatus SignalR send failed.");
            }
        }

        public async Task<DocumentBuilderTurnResult> UndoAsync(int userId, CancellationToken ct)
        {
            var session = await this.sessions.GetOrCreateAsync(userId, ct);
            if (session.UndoInverses.Count == 0)
                return new DocumentBuilderTurnResult { Version = session.Draft.Version, Ops = Array.Empty<DraftPatchOp>(), Draft = session.Draft };

            var inverse = session.UndoInverses[^1];
            var forward = session.UndoForwards[^1];
            session.UndoInverses.RemoveAt(session.UndoInverses.Count - 1);
            session.UndoForwards.RemoveAt(session.UndoForwards.Count - 1);
            session.RedoInverses.Add(inverse);
            session.RedoForwards.Add(forward);
            session.Draft = DraftPatcher.Revert(session.Draft, inverse);
            await this.sessions.SaveAsync(session, ct);
            return new DocumentBuilderTurnResult { Version = session.Draft.Version, Ops = inverse, Draft = session.Draft };
        }

        public async Task<DocumentBuilderTurnResult> RedoAsync(int userId, CancellationToken ct)
        {
            var session = await this.sessions.GetOrCreateAsync(userId, ct);
            if (session.RedoForwards.Count == 0)
                return new DocumentBuilderTurnResult { Version = session.Draft.Version, Ops = Array.Empty<DraftPatchOp>(), Draft = session.Draft };

            var forward = session.RedoForwards[^1];
            var inverse = session.RedoInverses[^1];
            session.RedoForwards.RemoveAt(session.RedoForwards.Count - 1);
            session.RedoInverses.RemoveAt(session.RedoInverses.Count - 1);
            var (next, _) = DraftPatcher.Apply(session.Draft, session.Draft.Version, forward);
            session.Draft = next;
            session.UndoForwards.Add(forward);
            session.UndoInverses.Add(inverse);
            await this.sessions.SaveAsync(session, ct);
            return new DocumentBuilderTurnResult { Version = next.Version, Ops = forward, Draft = next };
        }
    }
}
