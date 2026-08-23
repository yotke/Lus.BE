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
    /// <summary>
    /// End-to-end passes over the whole builder flow, driven by the shape of the real
    /// exemplar (רשות שדות התעופה hours account): import an exemplar, answer the interview,
    /// correct a cell by hand, and check the document that comes out is one you could sign.
    ///
    /// These are the cases that keep catching regressions the unit tests miss, because each
    /// one crosses agents, the patcher, the totals calculator and the version guard together.
    /// </summary>
    public class DocumentFlowTests
    {
        private const int User = 900;

        [Fact]
        public async Task Import_then_interview_then_edit_produces_a_signable_document()
        {
            var orchestrator = Create(FlowPython().Object);

            // 1. The exemplar teaches the shape and the letterhead.
            var imported = await orchestrator.ImportTemplateAsync(User, "/tmp/exemplar.xlsx", CancellationToken.None);
            Assert.Equal("תדם", imported.Draft.Template?.ClientName);
            Assert.Equal("01032601", imported.Draft.AccountNumber);
            Assert.True(imported.Draft.Template?.Rtl);

            // 2. Dictation adds a work row, attributed to the agent that extracted it.
            var dictated = await orchestrator.RunTurnAsync(
                User, imported.Version, "5 במרץ 3 שעות במשרד — התייעצות", CancellationToken.None);
            Assert.Single(dictated.Draft.Rows);
            Assert.Equal("doc.row_extractor", dictated.Draft.Rows[0].Source);

            // 3. The carry-in continues the previous month (the exemplar's 810 -> 760 chain).
            var carried = await orchestrator.RunTurnAsync(
                User, dictated.Version, "760", "carry_in", CancellationToken.None);
            Assert.Equal(760m, carried.Draft.Totals.CarryIn);
            Assert.Equal(757m, carried.Draft.Totals.Remaining);

            // 4. The rate answer prices it — and is recorded as the user's own value.
            var priced = await orchestrator.RunTurnAsync(
                User, carried.Version, "225", "hourly_rate", CancellationToken.None);
            Assert.Equal(225m, priced.Draft.Totals.HourlyRate);
            Assert.Equal(796.50m, priced.Draft.Totals.Total);

            // 5. A hand correction on the canvas, through the same patch path.
            var corrected = await orchestrator.ApplyCanvasEditAsync(User, priced.Version, new[]
            {
                new DraftPatchOp
                {
                    Op = "UpdateRow",
                    Path = "rows[0].hours",
                    Value = System.Text.Json.JsonSerializer.SerializeToElement(new { Hours = 4m })
                }
            }, CancellationToken.None);

            Assert.Equal("user", corrected.Draft.Rows[0].Source);
            Assert.Equal(4m, corrected.Draft.Totals.Hours);
            // Money follows the corrected hours: 4 × 225 × 1.18
            Assert.Equal(1062.00m, corrected.Draft.Totals.Total);
            Assert.Equal(756m, corrected.Draft.Totals.Remaining);
        }

        [Fact]
        public async Task Undo_walks_the_whole_flow_back()
        {
            var orchestrator = Create(FlowPython().Object);
            await orchestrator.ImportTemplateAsync(901, "/tmp/exemplar.xlsx", CancellationToken.None);
            var dictated = await orchestrator.RunTurnAsync(901, 1, "3 שעות", CancellationToken.None);
            await orchestrator.RunTurnAsync(901, dictated.Version, "225", "hourly_rate", CancellationToken.None);

            var back1 = await orchestrator.UndoAsync(901, CancellationToken.None);
            Assert.Null(back1.Draft.Totals.HourlyRate);

            var back2 = await orchestrator.UndoAsync(901, CancellationToken.None);
            Assert.Empty(back2.Draft.Rows);

            var back3 = await orchestrator.UndoAsync(901, CancellationToken.None);
            Assert.Equal(0, back3.Version);
        }

        [Fact]
        public async Task Redo_restores_a_priced_document()
        {
            var orchestrator = Create(FlowPython().Object);
            await orchestrator.RunTurnAsync(902, 0, "3 שעות", CancellationToken.None);
            await orchestrator.RunTurnAsync(902, 1, "225", "hourly_rate", CancellationToken.None);
            await orchestrator.UndoAsync(902, CancellationToken.None);

            var redone = await orchestrator.RedoAsync(902, CancellationToken.None);

            Assert.Equal(225m, redone.Draft.Totals.HourlyRate);
        }

        [Fact]
        public async Task Removing_the_last_row_zeroes_the_money_without_zeroing_the_rate()
        {
            var orchestrator = Create(FlowPython().Object);
            await orchestrator.RunTurnAsync(903, 0, "3 שעות", CancellationToken.None);
            var priced = await orchestrator.RunTurnAsync(903, 1, "225", "hourly_rate", CancellationToken.None);

            var emptied = await orchestrator.ApplyCanvasEditAsync(903, priced.Version, new[]
            {
                new DraftPatchOp { Op = "RemoveRow", Path = "rows[0]" }
            }, CancellationToken.None);

            Assert.Empty(emptied.Draft.Rows);
            Assert.Equal(0m, emptied.Draft.Totals.Hours);
            Assert.Equal(225m, emptied.Draft.Totals.HourlyRate);
            Assert.Equal(0m, emptied.Draft.Totals.Total);
        }

        [Fact]
        public async Task Two_dictated_rows_sum_into_the_totals_band()
        {
            var orchestrator = Create(FlowPython().Object);
            var first = await orchestrator.RunTurnAsync(904, 0, "3 שעות", CancellationToken.None);
            var second = await orchestrator.RunTurnAsync(904, first.Version, "2 שעות", CancellationToken.None);

            Assert.Equal(2, second.Draft.Rows.Count);
            Assert.Equal(5m, second.Draft.Totals.Hours);
        }

        [Fact]
        public async Task A_dictated_date_derives_the_document_day_letter()
        {
            var orchestrator = Create(DatedRowPython().Object);

            var result = await orchestrator.RunTurnAsync(905, 0, "5 במרץ", CancellationToken.None);

            // 2026-03-05 is a Thursday; the workbook writes 'ה', the 5th day letter.
            Assert.Equal(5, result.Draft.Rows[0].DayOfWeek);
        }

        [Fact]
        public async Task A_stale_canvas_edit_never_silently_overwrites()
        {
            var orchestrator = Create(FlowPython().Object);
            await orchestrator.RunTurnAsync(906, 0, "3 שעות", CancellationToken.None);
            await orchestrator.RunTurnAsync(906, 1, "2 שעות", CancellationToken.None);

            // Version 1 is two turns behind by now.
            await Assert.ThrowsAsync<DraftVersionConflictException>(
                () => orchestrator.ApplyCanvasEditAsync(906, 1, new[]
                {
                    new DraftPatchOp
                    {
                        Op = "UpdateRow",
                        Path = "rows[0].hours",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement(new { Hours = 99m })
                    }
                }, CancellationToken.None));
        }

        [Fact]
        public async Task A_failing_agent_leaves_the_previous_document_intact()
        {
            var orchestrator = Create(FlowPython().Object);
            var good = await orchestrator.RunTurnAsync(907, 0, "3 שעות", CancellationToken.None);

            var python = new Mock<IPythonScriptsAdapter>();
            python.Setup(p => p.RunAgentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Ok\":false,\"Agent\":\"doc.row_extractor\",\"SchemaVersion\":1,\"Result\":null,\"ErrorInfo\":{\"Code\":\"agent_error\",\"UserMessage\":\"x\",\"UserMessageEn\":\"x\"}}");

            Assert.Single(good.Draft.Rows);
        }

        [Fact]
        public async Task A_canvas_edit_moves_the_interview_along()
        {
            var orchestrator = Create(AskingPython().Object);
            await orchestrator.RunTurnAsync(908, 0, "3 שעות", CancellationToken.None);

            var edited = await orchestrator.ApplyCanvasEditAsync(908, 1, new[]
            {
                new DraftPatchOp
                {
                    Op = "UpdateRow",
                    Path = "rows[0].location",
                    Value = System.Text.Json.JsonSerializer.SerializeToElement(new { Location = "שטח" })
                }
            }, CancellationToken.None);

            // Filling a cell by hand must refresh the question, not leave a stale one on screen.
            Assert.NotNull(edited.Question);
            Assert.Equal("hourly_rate", edited.Question!.Id);
        }

        [Fact]
        public async Task A_canvas_edit_reports_validator_findings()
        {
            var orchestrator = Create(WarningPython().Object);
            await orchestrator.RunTurnAsync(909, 0, "3 שעות", CancellationToken.None);

            var edited = await orchestrator.ApplyCanvasEditAsync(909, 1, new[]
            {
                new DraftPatchOp
                {
                    Op = "UpdateRow",
                    Path = "rows[0].hours",
                    Value = System.Text.Json.JsonSerializer.SerializeToElement(new { Hours = 5m })
                }
            }, CancellationToken.None);

            Assert.Single(edited.Warnings);
            Assert.Equal("empty_rate", edited.Warnings[0].Code);
        }

        /// <summary>Planner that always has a next question.</summary>
        private static Mock<IPythonScriptsAdapter> AskingPython() => Customized(
            planner: "{\"Question\":{\"Id\":\"hourly_rate\",\"Text\":\"מה התעריף?\",\"Chips\":[\"225\"]}}");

        /// <summary>Validator that always reports the unpriced-document warning.</summary>
        private static Mock<IPythonScriptsAdapter> WarningPython() => Customized(
            validator: "{\"Ok\":false,\"Warnings\":[{\"Code\":\"empty_rate\",\"Message\":\"חסר תעריף\"}],\"Patches\":[]}");

        private static Mock<IPythonScriptsAdapter> Customized(string? planner = null, string? validator = null)
        {
            var python = FlowPython();
            if (planner is not null)
                python.Setup(p => p.RunAgentAsync("doc.question_planner", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("{\"Ok\":true,\"Agent\":\"doc.question_planner\",\"SchemaVersion\":1,\"Result\":" + planner + ",\"ErrorInfo\":null}");
            if (validator is not null)
                python.Setup(p => p.RunAgentAsync("doc.validator", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("{\"Ok\":true,\"Agent\":\"doc.validator\",\"SchemaVersion\":1,\"Result\":" + validator + ",\"ErrorInfo\":null}");
            return python;
        }

        // ── fixtures ────────────────────────────────────────────────────────────────

        /// <summary>Python stand-in shaped like the real doc.* agents.</summary>
        private static Mock<IPythonScriptsAdapter> FlowPython() => AgentPython(
            rowPatch: "{\"Op\":\"AddRow\",\"Path\":\"rows\",\"Value\":{\"Hours\":HOURS,\"Subject\":\"עבודה\",\"Location\":\"משרד\"}}");

        private static Mock<IPythonScriptsAdapter> DatedRowPython() => AgentPython(
            rowPatch: "{\"Op\":\"AddRow\",\"Path\":\"rows\",\"Value\":{\"Date\":\"2026-03-05\",\"Hours\":HOURS,\"Subject\":\"עבודה\",\"Location\":\"משרד\"}}");

        private static Mock<IPythonScriptsAdapter> AgentPython(string rowPatch)
        {
            var python = new Mock<IPythonScriptsAdapter>();
            python.Setup(p => p.RunAgentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string agent, string _, string input, string __, CancellationToken ___) => Task.FromResult(agent switch
                {
                    "doc.template_reader" =>
                        "{\"Ok\":true,\"Agent\":\"doc.template_reader\",\"SchemaVersion\":1,\"Result\":{\"Patches\":[" +
                        "{\"Op\":\"SetField\",\"Path\":\"template.rtl\",\"Value\":true}," +
                        "{\"Op\":\"SetField\",\"Path\":\"template.clientName\",\"Value\":\"תדם\"}," +
                        "{\"Op\":\"SetField\",\"Path\":\"template.dataBandStartRow\",\"Value\":11}," +
                        "{\"Op\":\"SetField\",\"Path\":\"accountNumber\",\"Value\":\"01032601\"}" +
                        "]},\"ErrorInfo\":null}",

                    "doc.validator" =>
                        "{\"Ok\":true,\"Agent\":\"doc.validator\",\"SchemaVersion\":1,\"Result\":{\"Ok\":true,\"Warnings\":[],\"Patches\":[]},\"ErrorInfo\":null}",

                    "doc.question_planner" =>
                        "{\"Ok\":true,\"Agent\":\"doc.question_planner\",\"SchemaVersion\":1,\"Result\":{\"Question\":null},\"ErrorInfo\":null}",

                    "doc.advisor" =>
                        "{\"Ok\":true,\"Agent\":\"doc.advisor\",\"SchemaVersion\":1,\"Result\":{\"Answer\":\"\",\"Suggestions\":[]},\"ErrorInfo\":null}",

                    "doc.row_extractor" =>
                        "{\"Ok\":true,\"Agent\":\"doc.row_extractor\",\"SchemaVersion\":1,\"Result\":{\"Patches\":[" +
                        rowPatch.Replace("HOURS", input.Contains("\"Text\":\"2") ? "2" : "3") +
                        "],\"Notes\":[]},\"ErrorInfo\":null}",

                    // Every other content agent contributes nothing this turn.
                    _ => "{\"Ok\":true,\"Agent\":\"" + agent + "\",\"SchemaVersion\":1,\"Result\":{\"Patches\":[],\"Notes\":[],\"Columns\":[]},\"ErrorInfo\":null}",
                }));
            return python;
        }

        private static DocumentBuilderOrchestrator Create(IPythonScriptsAdapter python)
        {
            var bag = new Dictionary<string, DocumentBuildSession>();
            var cache = new Mock<EasyCaching.Core.IEasyCachingProvider>();
            cache.Setup(c => c.GetAsync<DocumentBuildSession>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string key, CancellationToken _) =>
                    Task.FromResult(bag.TryGetValue(key, out var session)
                        ? new EasyCaching.Core.CacheValue<DocumentBuildSession>(session, true)
                        : new EasyCaching.Core.CacheValue<DocumentBuildSession>(null!, false)));
            cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<DocumentBuildSession>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback<string, DocumentBuildSession, TimeSpan, CancellationToken>((key, session, _, _) => bag[key] = session)
                .Returns(Task.CompletedTask);

            var store = new Lus.Application.Common.Services.SelfHealingStore(cache.Object);
            var sessions = new DocumentBuildSessionStore(store, cache.Object, NullLogger<DocumentBuildSessionStore>.Instance);
            var client = new DocumentBuilderAgentClient(
                python, Options.Create(new AiBuilderOptions()), NullLogger<DocumentBuilderAgentClient>.Instance);

            return new DocumentBuilderOrchestrator(
                sessions,
                new DocumentBuilderAgentCatalog(),
                client,
                new NullDocumentBuilderEventSender(NullLogger<NullDocumentBuilderEventSender>.Instance),
                NullLogger<DocumentBuilderOrchestrator>.Instance);
        }
    }
}
