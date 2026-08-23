using System.Text.Json;
using Lus.Application.Documents.Builder.Services;
using Lus.Contracts.Documents.Builder;
using Xunit;

namespace Lus.Api.Tests.Documents
{
    public class DocumentTotalsCalculatorTests
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        [Fact]
        public void Empty_rate_does_not_invent_zero_total()
        {
            var draft = new DocumentDraftDto
            {
                Rows = { new DocumentDraftRowDto { Hours = 32 } },
                Totals = new DocumentTotalsDto { HourlyRate = null, VatPercent = 18 }
            };

            var op = DocumentTotalsCalculator.Diff(draft);
            Assert.NotNull(op);
            Assert.True(op!.Value.HasValue);
            var totals = JsonSerializer.Deserialize<DocumentTotalsDto>(op.Value.Value.GetRawText(), Json);
            Assert.Equal(32m, totals!.Hours);
            Assert.Null(totals.Total);
        }

        [Fact]
        public void Rate_present_computes_vat_total()
        {
            var draft = new DocumentDraftDto
            {
                Rows = { new DocumentDraftRowDto { Hours = 10 } },
                Totals = new DocumentTotalsDto { HourlyRate = 100, VatPercent = 18 }
            };

            var op = DocumentTotalsCalculator.Diff(draft);
            Assert.NotNull(op);
            Assert.True(op!.Value.HasValue);
            var totals = JsonSerializer.Deserialize<DocumentTotalsDto>(op.Value.Value.GetRawText(), Json);
            Assert.Equal(1180m, totals!.Total);
        }
    }
}
