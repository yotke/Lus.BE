using Lus.Application.Common.Options;
using Lus.Application.Common.Ports;
using Lus.Application.Documents.Builder;
using Lus.Application.Documents.Builder.Agents;
using Lus.Application.Documents.Builder.Orchestration;
using Lus.Application.Documents.Builder.Services;
using Lus.Contracts.Documents.Builder;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lus.Api.Tests.Documents
{
    public class DocumentBuilderOrchestratorTests
    {
        [Fact]
        public async Task Turn_applies_row_patch_and_undo_restores_version_zero()
        {
            var python = RowExtractorPython();
            var orchestrator = Create(python.Object);

            var t1 = await orchestrator.RunTurnAsync(7, 0, "5 במרץ 3 שעות במשרד — התייעצות", CancellationToken.None);
            Assert.Equal(1, t1.Version);
            Assert.Contains(t1.Ops, o => o.Op == "AddRow");

            var t2 = await orchestrator.RunTurnAsync(7, 1, "8 במרץ 2 שעות במשרד — המשך", CancellationToken.None);
            Assert.Equal(2, t2.Version);
            Assert.Equal(2, t2.Draft.Rows.Count);

            var undone = await orchestrator.UndoAsync(7, CancellationToken.None);
            Assert.Equal(1, undone.Version);

            var undoneAgain = await orchestrator.UndoAsync(7, CancellationToken.None);
            Assert.Equal(0, undoneAgain.Version);
        }

        [Fact]
        public async Task Stale_turn_conflicts()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(9, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);
            await Assert.ThrowsAsync<DraftVersionConflictException>(
                () => orchestrator.RunTurnAsync(9, 0, "8 במרץ 2 שעות", CancellationToken.None));
        }

        [Fact]
        public async Task Failed_agent_does_not_bump_version()
        {
            var python = new Mock<IPythonScriptsAdapter>();
            python.Setup(p => p.RunAgentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Ok\":false,\"Agent\":\"doc.row_extractor\",\"SchemaVersion\":1,\"Result\":null,\"ErrorInfo\":{\"Code\":\"agent_error\",\"UserMessage\":\"x\",\"UserMessageEn\":\"x\"}}");

            var orchestrator = Create(python.Object);
            var result = await orchestrator.RunTurnAsync(3, 0, "bad", CancellationToken.None);
            Assert.Equal(0, result.Version);
            Assert.Empty(result.Ops);
        }

        [Fact]
        public async Task Throwing_event_sender_does_not_fail_the_turn()
        {
            var events = new ThrowingSender();
            var orchestrator = Create(RowExtractorPython().Object, events);
            var result = await orchestrator.RunTurnAsync(4, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);
            Assert.Equal(1, result.Version);
            Assert.NotEmpty(result.Ops);
        }

        [Fact]
        public async Task Redo_reapplies_the_undone_batch()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(5, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);
            await orchestrator.UndoAsync(5, CancellationToken.None);
            var redone = await orchestrator.RedoAsync(5, CancellationToken.None);
            Assert.Equal(1, redone.Version);
            Assert.Single(redone.Draft.Rows);
        }

        [Fact]
        public async Task Canvas_edit_goes_through_the_same_patch_path_as_an_agent()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(21, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            var edited = await orchestrator.ApplyCanvasEditAsync(21, 1, new[]
            {
                new DraftPatchOp
                {
                    Op = "UpdateRow",
                    Path = "rows[0]",
                    Value = System.Text.Json.JsonSerializer.SerializeToElement(new { Hours = 7m })
                }
            }, CancellationToken.None);

            Assert.Equal(2, edited.Version);
            Assert.Equal(7m, edited.Draft.Rows[0].Hours);

            // Same history as an agent turn: the hand edit is undoable.
            var undone = await orchestrator.UndoAsync(21, CancellationToken.None);
            Assert.Equal(1, undone.Version);
            Assert.Equal(3m, undone.Draft.Rows[0].Hours);
        }

        [Fact]
        public async Task Canvas_edit_recomputes_derived_totals()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(22, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            var edited = await orchestrator.ApplyCanvasEditAsync(22, 1, new[]
            {
                new DraftPatchOp
                {
                    Op = "UpdateRow",
                    Path = "rows[0]",
                    Value = System.Text.Json.JsonSerializer.SerializeToElement(new { Hours = 9m })
                }
            }, CancellationToken.None);

            Assert.Equal(9m, edited.Draft.Totals.Hours);
        }

        [Fact]
        public async Task Canvas_edit_refuses_to_write_a_derived_cell()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(23, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => orchestrator.ApplyCanvasEditAsync(23, 1, new[]
                {
                    new DraftPatchOp
                    {
                        Op = "SetField",
                        Path = "totals.hours",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement(999m)
                    }
                }, CancellationToken.None));
        }

        [Fact]
        public async Task Canvas_edit_conflicts_on_a_stale_version()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(24, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            await Assert.ThrowsAsync<DraftVersionConflictException>(
                () => orchestrator.ApplyCanvasEditAsync(24, 0, new[]
                {
                    new DraftPatchOp
                    {
                        Op = "UpdateRow",
                        Path = "rows[0]",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement(new { Hours = 1m })
                    }
                }, CancellationToken.None));
        }

        [Fact]
        public async Task Importing_an_exemplar_learns_the_template_onto_the_draft()
        {
            var python = new Mock<IPythonScriptsAdapter>();
            python.Setup(p => p.RunAgentAsync("doc.template_reader", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    "{\"Ok\":true,\"Agent\":\"doc.template_reader\",\"SchemaVersion\":1,\"Result\":{\"Patches\":[" +
                    "{\"Op\":\"SetField\",\"Path\":\"template.rtl\",\"Value\":true}," +
                    "{\"Op\":\"SetField\",\"Path\":\"template.dataBandStartRow\",\"Value\":12}," +
                    "{\"Op\":\"SetField\",\"Path\":\"template.mergePolicy\",\"Value\":\"group-date-columns-AB\"}" +
                    "]},\"ErrorInfo\":null}");

            var orchestrator = Create(python.Object);
            var result = await orchestrator.ImportTemplateAsync(31, "/tmp/exemplar.xlsx", CancellationToken.None);

            Assert.Equal(1, result.Version);
            Assert.NotNull(result.Draft.Template);
            Assert.True(result.Draft.Template!.Rtl);
            Assert.Equal(12, result.Draft.Template.DataBandStartRow);
            Assert.Equal("group-date-columns-AB", result.Draft.Template.MergePolicy);
        }

        [Fact]
        public async Task A_failed_import_leaves_the_draft_untouched()
        {
            var python = new Mock<IPythonScriptsAdapter>();
            python.Setup(p => p.RunAgentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Ok\":false,\"Agent\":\"doc.template_reader\",\"SchemaVersion\":1,\"Result\":null,\"ErrorInfo\":{\"Code\":\"invalid_input\",\"UserMessage\":\"x\",\"UserMessageEn\":\"x\"}}");

            var orchestrator = Create(python.Object);
            var result = await orchestrator.ImportTemplateAsync(32, "/tmp/missing.xlsx", CancellationToken.None);

            Assert.Equal(0, result.Version);
            Assert.Null(result.Draft.Template);
        }

        [Fact]
        public async Task A_turn_surfaces_the_planner_question_and_validator_warnings()
        {
            var python = new Mock<IPythonScriptsAdapter>();
            python.Setup(p => p.RunAgentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string agent, string _, string __, string ___, CancellationToken ____) => agent switch
                {
                    "doc.question_planner" => Task.FromResult(
                        "{\"Ok\":true,\"Agent\":\"doc.question_planner\",\"SchemaVersion\":1,\"Result\":{\"Question\":{\"Id\":\"rate\",\"Text\":\"מה התעריף לשעה?\",\"Chips\":[\"120\",\"150\"]}},\"ErrorInfo\":null}"),
                    "doc.validator" => Task.FromResult(
                        "{\"Ok\":true,\"Agent\":\"doc.validator\",\"SchemaVersion\":1,\"Result\":{\"Ok\":false,\"Warnings\":[{\"Code\":\"missing_rate\",\"Message\":\"אין תעריף\"}],\"Patches\":[]},\"ErrorInfo\":null}"),
                    "doc.advisor" => Task.FromResult(
                        "{\"Ok\":true,\"Agent\":\"doc.advisor\",\"SchemaVersion\":1,\"Result\":{\"Answer\":\"אפשר להוסיף תעריף\",\"Suggestions\":[\"הוסף תעריף\"]},\"ErrorInfo\":null}"),
                    _ => Task.FromResult(
                        "{\"Ok\":true,\"Agent\":\"" + agent + "\",\"SchemaVersion\":1,\"Result\":{\"Patches\":[],\"Notes\":[]},\"ErrorInfo\":null}"),
                });

            var orchestrator = Create(python.Object);
            var result = await orchestrator.RunTurnAsync(41, 0, "שאלה", CancellationToken.None);

            Assert.NotNull(result.Question);
            Assert.Equal("rate", result.Question!.Id);
            Assert.Contains("120", result.Question.Chips);
            Assert.Single(result.Warnings);
            Assert.Equal("missing_rate", result.Warnings[0].Code);
            Assert.Single(result.Messages);
            Assert.Equal("אפשר להוסיף תעריף", result.Messages[0].Text);
        }

        // ── Regression cases taken from the real shipped exemplars ──────────────────
        // "חשבון שעות לפרויקטים ר.ש.ת 2026" + the Feb/Mar/Apr 2026 invoices: every monthly
        // sheet left the rate cell empty, so three issued PDFs bill 0.00 for 154 hours.

        [Fact]
        public async Task The_rate_is_an_input_and_can_be_entered()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(51, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            var priced = await orchestrator.ApplyCanvasEditAsync(51, 1, new[]
            {
                new DraftPatchOp
                {
                    Op = "SetField",
                    Path = "totals.hourlyRate",
                    Value = System.Text.Json.JsonSerializer.SerializeToElement(225m)
                }
            }, CancellationToken.None);

            Assert.Equal(225m, priced.Draft.Totals.HourlyRate);
            // 3h × 225 × 1.18
            Assert.Equal(796.50m, priced.Draft.Totals.Total);
        }

        [Fact]
        public async Task An_empty_rate_leaves_the_total_null_rather_than_zero()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            var result = await orchestrator.RunTurnAsync(52, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            Assert.Equal(3m, result.Draft.Totals.Hours);
            Assert.Null(result.Draft.Totals.HourlyRate);
            Assert.Null(result.Draft.Totals.Total);
        }

        [Fact]
        public async Task Carry_in_is_an_input_and_drives_the_remaining_balance()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(53, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            // The exemplar's chain: each month starts from the previous month's remainder.
            var carried = await orchestrator.ApplyCanvasEditAsync(53, 1, new[]
            {
                new DraftPatchOp
                {
                    Op = "SetField",
                    Path = "totals.carryIn",
                    Value = System.Text.Json.JsonSerializer.SerializeToElement(760m)
                }
            }, CancellationToken.None);

            Assert.Equal(760m, carried.Draft.Totals.CarryIn);
            Assert.Equal(757m, carried.Draft.Totals.Remaining);
        }

        [Theory]
        [InlineData("totals.hours")]
        [InlineData("totals.remaining")]
        [InlineData("totals.total")]
        public async Task Derived_totals_stay_read_only(string path)
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(54, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => orchestrator.ApplyCanvasEditAsync(54, 1, new[]
                {
                    new DraftPatchOp
                    {
                        Op = "SetField",
                        Path = path,
                        Value = System.Text.Json.JsonSerializer.SerializeToElement(999m)
                    }
                }, CancellationToken.None));
        }

        // ── Answering a question must close the loop ────────────────────────────────
        // Reported from the running app: the planner asked for the rate, the user typed
        // "225", and the answer was fed to the content wave as if it were dictation — so the
        // rate stayed empty and the same question came back, over and over.

        [Fact]
        public async Task Answering_the_rate_question_sets_the_rate()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(61, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            var answered = await orchestrator.RunTurnAsync(61, 1, "225", "hourly_rate", CancellationToken.None);

            Assert.Equal(225m, answered.Draft.Totals.HourlyRate);
            Assert.Equal(796.50m, answered.Draft.Totals.Total);
        }

        [Fact]
        public async Task Answering_a_question_does_not_add_a_row()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(62, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);
            var before = (await orchestrator.GetSessionAsync(62, CancellationToken.None)).Draft.Rows.Count;

            var answered = await orchestrator.RunTurnAsync(62, 1, "225", "hourly_rate", CancellationToken.None);

            Assert.Equal(before, answered.Draft.Rows.Count);
        }

        [Theory]
        [InlineData("225", 225)]
        [InlineData("תעריף 225", 225)]
        [InlineData("223.97", 223.97)]
        [InlineData("225 ש\"ח לשעה", 225)]
        public async Task A_rate_answer_is_read_out_of_ordinary_phrasing(string answer, double expected)
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(63, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            var answered = await orchestrator.RunTurnAsync(63, 1, answer, "hourly_rate", CancellationToken.None);

            Assert.Equal((decimal)expected, answered.Draft.Totals.HourlyRate);
        }

        [Fact]
        public async Task An_unbindable_question_falls_through_to_a_normal_turn()
        {
            var orchestrator = Create(RowExtractorPython().Object);

            // "first_row" wants dictation, not a field value — the content wave must still run.
            var result = await orchestrator.RunTurnAsync(64, 0, "3 שעות במשרד", "first_row", CancellationToken.None);

            Assert.NotEmpty(result.Draft.Rows);
        }

        // ── Provenance ─────────────────────────────────────────────────────────────
        [Fact]
        public async Task An_agent_row_records_the_agent_that_wrote_it()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            var result = await orchestrator.RunTurnAsync(65, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            Assert.Equal("doc.row_extractor", result.Draft.Rows[0].Source);
            Assert.NotNull(result.Draft.Rows[0].ChangedAt);
        }

        [Fact]
        public async Task A_hand_edit_records_the_user_even_on_a_single_field()
        {
            var orchestrator = Create(RowExtractorPython().Object);
            await orchestrator.RunTurnAsync(66, 0, "5 במרץ 3 שעות במשרד", CancellationToken.None);

            var edited = await orchestrator.ApplyCanvasEditAsync(66, 1, new[]
            {
                new DraftPatchOp
                {
                    Op = "UpdateRow",
                    Path = "rows[0].hours",
                    Value = System.Text.Json.JsonSerializer.SerializeToElement(new { Hours = 7m })
                }
            }, CancellationToken.None);

            Assert.Equal("user", edited.Draft.Rows[0].Source);
            Assert.Equal(7m, edited.Draft.Rows[0].Hours);
        }

        private static Mock<IPythonScriptsAdapter> RowExtractorPython()
        {
            var python = new Mock<IPythonScriptsAdapter>();
            python.Setup(p => p.RunAgentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string agent, string _, string input, string __, CancellationToken ___) =>
                {
                    if (agent is "doc.formatter")
                    {
                        return Task.FromResult(
                            "{\"Ok\":true,\"Agent\":\"doc.formatter\",\"SchemaVersion\":1,\"Result\":{\"Patches\":[{\"Op\":\"SetTotals\",\"Path\":\"totals\",\"Value\":{\"Hours\":3,\"CarryIn\":0,\"Remaining\":-3,\"VatPercent\":18}}],\"Totals\":{}},\"ErrorInfo\":null}");
                    }
                    // The validator/planner/advisor now run in every turn. They have their own
                    // result shapes, so they must not fall through to the row_extractor branch
                    // below — a catch-all there makes the validator emit a phantom row.
                    if (agent is "doc.validator")
                    {
                        return Task.FromResult(
                            "{\"Ok\":true,\"Agent\":\"doc.validator\",\"SchemaVersion\":1,\"Result\":{\"Ok\":true,\"Warnings\":[],\"Patches\":[]},\"ErrorInfo\":null}");
                    }
                    if (agent is "doc.question_planner")
                    {
                        return Task.FromResult(
                            "{\"Ok\":true,\"Agent\":\"doc.question_planner\",\"SchemaVersion\":1,\"Result\":{\"Question\":null},\"ErrorInfo\":null}");
                    }
                    if (agent is "doc.advisor")
                    {
                        return Task.FromResult(
                            "{\"Ok\":true,\"Agent\":\"doc.advisor\",\"SchemaVersion\":1,\"Result\":{\"Answer\":\"\",\"Suggestions\":[]},\"ErrorInfo\":null}");
                    }
                    if (agent is "doc.schema_planner" or "doc.reviewer" or "doc.carry_forward")
                    {
                        return Task.FromResult(
                            "{\"Ok\":true,\"Agent\":\"" + agent + "\",\"SchemaVersion\":1,\"Result\":{\"Patches\":[],\"Notes\":[],\"Columns\":[]},\"ErrorInfo\":null}");
                    }
                    var hours = input.Contains("2 שעות") ? 2 : 3;
                    return Task.FromResult(
                        "{\"Ok\":true,\"Agent\":\"doc.row_extractor\",\"SchemaVersion\":1,\"Result\":{\"Patches\":[{\"Op\":\"AddRow\",\"Path\":\"rows\",\"Value\":{\"Hours\":" + hours + ",\"Subject\":\"x\",\"Location\":\"משרד\"}}],\"Notes\":[]},\"ErrorInfo\":null}");
                });
            return python;
        }

        private static DocumentBuilderOrchestrator Create(
            IPythonScriptsAdapter python,
            IDocumentBuilderEventSender? events = null)
        {
            var bag = new Dictionary<string, DocumentBuildSession>();
            var cache = new Mock<EasyCaching.Core.IEasyCachingProvider>();
            cache.Setup(c => c.GetAsync<DocumentBuildSession>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string key, CancellationToken _) =>
                {
                    if (bag.TryGetValue(key, out var session))
                        return Task.FromResult(new EasyCaching.Core.CacheValue<DocumentBuildSession>(session, true));
                    return Task.FromResult(new EasyCaching.Core.CacheValue<DocumentBuildSession>(null!, false));
                });
            cache.Setup(c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<DocumentBuildSession>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, DocumentBuildSession, TimeSpan, CancellationToken>((key, session, _, _) => bag[key] = session)
                .Returns(Task.CompletedTask);

            var store = new Lus.Application.Common.Services.SelfHealingStore(cache.Object);
            var sessions = new DocumentBuildSessionStore(store, cache.Object, NullLogger<DocumentBuildSessionStore>.Instance);
            var options = Options.Create(new AiBuilderOptions());
            var client = new DocumentBuilderAgentClient(python, options, NullLogger<DocumentBuilderAgentClient>.Instance);
            return new DocumentBuilderOrchestrator(
                sessions,
                new DocumentBuilderAgentCatalog(),
                client,
                events ?? new NullDocumentBuilderEventSender(NullLogger<NullDocumentBuilderEventSender>.Instance),
                NullLogger<DocumentBuilderOrchestrator>.Instance);
        }

        private sealed class ThrowingSender : IDocumentBuilderEventSender
        {
            public Task SendDraftPatchedAsync(string jobId, string userId, string sessionId, int version,
                IReadOnlyList<DraftPatchOp> ops, CancellationToken ct = default)
                => throw new InvalidOperationException("hub down");

            public Task SendAgentStatusAsync(string jobId, string userId, string agent, string state,
                string? detail = null, CancellationToken ct = default)
                => throw new InvalidOperationException("hub down");

            public Task SendQuestionAskedAsync(string jobId, string userId, DocumentQuestionDto question,
                CancellationToken ct = default) => Task.CompletedTask;

            public Task SendBuilderMessageAsync(string jobId, string userId, string role, string text,
                CancellationToken ct = default, IReadOnlyList<string>? suggestions = null) => Task.CompletedTask;

            public Task SendCommitCompletedAsync(string jobId, string userId, string sessionId, int organizationId,
                IReadOnlyDictionary<string, int> counts, IReadOnlyList<DocumentWarningDto> warnings,
                CancellationToken ct = default) => Task.CompletedTask;

            public Task SendErrorAsync(string jobId, string userId, string errorCode, string userSafeMessage,
                CancellationToken ct = default) => Task.CompletedTask;
        }
    }
}
