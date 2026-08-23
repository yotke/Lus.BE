using Lus.Application.Common;

namespace Lus.Application.Documents.Entities
{
    /// <summary>Rates out of the stale sheet and into storage. Never invented by an LLM.</summary>
    public class RateCard : EntityBase<int>
    {
        public int SeriesId { get; set; }

        public DocumentSeries? Series { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public decimal HourlyRate { get; set; }

        public decimal VatPercent { get; set; } = 18;

        public decimal? PlotsPercent { get; set; }
    }
}
