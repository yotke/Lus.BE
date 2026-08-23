using Lus.Application.Common;
using Lus.Contracts.Documents;

namespace Lus.Application.Documents.Entities
{
    /// <summary>Derived from the exemplar; the draft skeleton. JSON blobs are LONGTEXT.</summary>
    public class DocumentTemplate : EntityBase<int>
    {
        public int SeriesId { get; set; }

        public DocumentSeries? Series { get; set; }

        public string Fingerprint { get; set; } = "";

        public bool Rtl { get; set; } = true;

        public string ColumnWidths { get; set; } = "[]";

        public string LetterheadFields { get; set; } = "{}";

        public string TableHeader { get; set; } = "[]";

        public int DataBandStartRow { get; set; }

        public int DataBandLevels { get; set; } = 1;

        public string DataBandMergePolicy { get; set; } = "{}";

        public int? BlockHeight { get; set; }

        public DocumentRepeatPolicy RepeatPolicy { get; set; } = DocumentRepeatPolicy.OnePerSheet;

        public string TotalsFormulaSet { get; set; } = "{}";

        public string BillingBlock { get; set; } = "{}";

        public string DeclarationBlock { get; set; } = "{}";

        public string? ContractBlock { get; set; }
    }
}
