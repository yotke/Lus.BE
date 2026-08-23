using System.Text.Json;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Services
{
    /// <summary>
    /// LLM-free money and hours. Empty rate never becomes 0.00 (the issued-PDF money bug).
    /// </summary>
    public static class DocumentTotalsCalculator
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static DraftPatchOp? Diff(DocumentDraftDto draft)
        {
            var hours = draft.Rows.Sum(r => r.Hours ?? 0m);
            var carryIn = draft.Totals.CarryIn;
            var remaining = carryIn - hours;
            decimal? total = null;
            if (draft.Totals.HourlyRate is { } rate)
            {
                var subtotal = hours * rate;
                if (draft.Totals.PlotsPercent is { } plots)
                    subtotal -= subtotal * (plots / 100m);
                total = decimal.Round(subtotal * (1 + draft.Totals.VatPercent / 100m), 2, MidpointRounding.AwayFromZero);
            }

            var next = new DocumentTotalsDto
            {
                Hours = hours,
                CarryIn = carryIn,
                Remaining = remaining,
                HourlyRate = draft.Totals.HourlyRate,
                VatPercent = draft.Totals.VatPercent,
                PlotsPercent = draft.Totals.PlotsPercent,
                Total = total
            };

            if (Same(draft.Totals, next))
                return null;

            return new DraftPatchOp
            {
                Op = "SetTotals",
                Path = "totals",
                Value = JsonSerializer.SerializeToElement(next, Json)
            };
        }

        private static bool Same(DocumentTotalsDto a, DocumentTotalsDto b) =>
            a.Hours == b.Hours
            && a.CarryIn == b.CarryIn
            && a.Remaining == b.Remaining
            && a.HourlyRate == b.HourlyRate
            && a.VatPercent == b.VatPercent
            && a.PlotsPercent == b.PlotsPercent
            && a.Total == b.Total;
    }
}
