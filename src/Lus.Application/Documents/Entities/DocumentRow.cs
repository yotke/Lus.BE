using Lus.Application.Common;

namespace Lus.Application.Documents.Entities
{
    /// <summary>
    /// A segment in the data band. Hours is derived from EndTime − StartTime when both are set.
    /// </summary>
    public class DocumentRow : EntityBase<int>
    {
        public int DayId { get; set; }

        public DocumentDay? Day { get; set; }

        public int Ordinal { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public decimal Hours { get; set; }

        public string? Location { get; set; }

        public string? Subject { get; set; }
    }
}
