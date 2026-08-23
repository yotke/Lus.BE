using Lus.Application.Common;

namespace Lus.Application.Documents.Entities
{
    /// <summary>
    /// Day grouping — archetype 2's merged day-total row. Archetype 1 is one segment per day.
    /// <see cref="DayOfWeek"/> and <see cref="TotalHours"/> are derived, never typed.
    /// </summary>
    public class DocumentDay : EntityBase<int>
    {
        public int InstanceId { get; set; }

        public DocumentInstance? Instance { get; set; }

        public DateTime Date { get; set; }

        public int DayOfWeek { get; set; }

        public decimal TotalHours { get; set; }

        public ICollection<DocumentRow> Rows { get; set; } = new List<DocumentRow>();
    }
}
