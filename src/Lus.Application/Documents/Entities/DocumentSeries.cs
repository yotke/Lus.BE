using Lus.Application.Common;
using Lus.Contracts.Documents;

namespace Lus.Application.Documents.Entities
{
    /// <summary>
    /// The workbook. Per client-project, open-ended — no year column.
    /// The year in the filename is a label; the balance chain may cross years.
    /// </summary>
    public class DocumentSeries : EntityBase<int>
    {
        public int? OrganizationId { get; set; }

        public string Name { get; set; } = "";

        public string ClientName { get; set; } = "";

        public DocumentTemplate? Template { get; set; }

        public int? ExemplarFileId { get; set; }

        public DocumentSourceFormat SourceFormat { get; set; } = DocumentSourceFormat.Xlsx;

        public ICollection<DocumentInstance> Instances { get; set; } = new List<DocumentInstance>();

        public ICollection<RateCard> RateCards { get; set; } = new List<RateCard>();
    }
}
