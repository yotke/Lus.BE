using Lus.FilterEngine.Operations;

namespace Lus.FilterEngine.Common
{
    public sealed class FilterParameter
    {
        public FilterParameter()
        {
            GroupingOperation = BooleanOperation.And;
            Operation = FilterOperation.Ct;
            IsNegated = false;
            InnerPath = null;
        }

        public BooleanOperation GroupingOperation { get; set; }

        public FilterOperation Operation { get; set; }

        public bool IsNegated { get; set; }

        public string? InnerPath { get; set; }

        public string?[] Values { get; set; }

        public bool? AsString { get; set; } = false;

    }
}
