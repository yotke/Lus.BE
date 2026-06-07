using Lus.FilterEngine.Operations;
using System.Globalization;
using System.Linq.Expressions;

namespace Lus.FilterEngine.Common
{
    public sealed class Filtering
    {
        public Filtering()
        {
            FilterParameters = new List<FilterParameter>();
            GroupingOperation = BooleanOperation.And;
        }

        public string PropertyName { get; set; }

        public BooleanOperation GroupingOperation { get; set; }

        public IEnumerable<FilterParameter> FilterParameters { get; set; }

        public static Filtering CreateFiltering(string propertyName, FilterOperation operation, string? innerPath, params string[] values)
        {
            return new Filtering
            {
                PropertyName = propertyName,
                FilterParameters = new List<FilterParameter>
                {
                    new FilterParameter
                    {
                        Operation = operation,
                        InnerPath = innerPath,
                        Values = values
                    }
                }
            };
        }

    }
}
