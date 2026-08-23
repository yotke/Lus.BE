using Lus.Application.Common;
using Lus.Contracts.Documents;

namespace Lus.Application.Documents.Entities
{
    /// <summary>
    /// One report block: a whole sheet in archetype 1, one of ~11 stacked blocks in archetype 2.
    /// </summary>
    public class DocumentInstance : EntityBase<int>
    {
        public int SeriesId { get; set; }

        public DocumentSeries? Series { get; set; }

        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public string SheetName { get; set; } = "";

        public int BlockOrdinal { get; set; }

        public int? BlockStartRow { get; set; }

        public string? AccountNumber { get; set; }

        public string? ProjectName { get; set; }

        public string? ContractNumber { get; set; }

        public DateTime? ContractValidFrom { get; set; }

        public DateTime? ContractValidTo { get; set; }

        public int? CarryInFromInstanceId { get; set; }

        public DocumentInstance? CarryInFrom { get; set; }

        public DocumentInstanceStatus Status { get; set; } = DocumentInstanceStatus.Draft;

        public ICollection<DocumentDay> Days { get; set; } = new List<DocumentDay>();
    }
}
