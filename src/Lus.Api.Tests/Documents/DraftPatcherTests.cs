using System.Text.Json;
using Lus.Application.Documents.Builder.Services;
using Lus.Contracts.Documents.Builder;
using Xunit;

namespace Lus.Api.Tests.Documents
{
    public class DraftPatcherTests
    {
        [Fact]
        public void AddRow_then_UpdateRow_hours_bumps_version()
        {
            var draft = new DocumentDraftDto { Version = 0 };
            var add = Op("AddRow", "rows", new DocumentDraftRowDto
            {
                Date = new DateTime(2026, 3, 5),
                Hours = 3,
                Subject = "התייעצות"
            });
            var (v1, _) = DraftPatcher.Apply(draft, 0, new[] { add });
            Assert.Equal(1, v1.Version);
            Assert.Single(v1.Rows);
            Assert.Equal("התייעצות", v1.Rows[0].Subject);
            Assert.Equal((int)DayOfWeek.Thursday + 1, v1.Rows[0].DayOfWeek);

            var update = Op("UpdateRow", "rows[0].hours", new DocumentDraftRowDto { Hours = 4 });
            var (v2, _) = DraftPatcher.Apply(v1, 1, new[] { update });
            Assert.Equal(2, v2.Version);
            Assert.Equal(4m, v2.Rows[0].Hours);
        }

        [Fact]
        public void Stale_version_is_rejected()
        {
            var draft = new DocumentDraftDto { Version = 1 };
            var op = Op("SetField", "lastUtterance", "hi");
            Assert.Throws<DraftVersionConflictException>(() => DraftPatcher.Apply(draft, 0, new[] { op }));
        }

        [Fact]
        public void Undo_restores_previous_draft()
        {
            var draft = new DocumentDraftDto { Version = 0 };
            var op = Op("SetField", "lastUtterance", "שלום");
            var (next, inverse) = DraftPatcher.Apply(draft, 0, new[] { op });
            Assert.Equal("שלום", next.LastUtterance);

            var undone = DraftPatcher.Revert(next, inverse);
            Assert.Equal(0, undone.Version);
            Assert.Equal("", undone.LastUtterance);
        }

        [Fact]
        public void DayOfWeek_is_derived_from_Date_never_typed()
        {
            var draft = new DocumentDraftDto { Version = 0 };
            var op = Op("AddRow", "rows", new DocumentDraftRowDto
            {
                Date = new DateTime(2026, 3, 5),
                DayOfWeek = 0,
                Subject = "x"
            });
            var (next, _) = DraftPatcher.Apply(draft, 0, new[] { op });
            Assert.Equal((int)DayOfWeek.Thursday + 1, next.Rows[0].DayOfWeek);
        }

        private static DraftPatchOp Op(string op, string path, object value) => new()
        {
            Op = op,
            Path = path,
            Value = JsonSerializer.SerializeToElement(value, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };
    }
}
