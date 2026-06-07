using Lus.FilterEngine.Common;
using Sorting = Lus.FilterEngine.Common.Sorting;

namespace Lus.FilterEngine.Converters
{
    public class DefaultSearchRequestConverter<TDto, T> : ISearchRequestConverter<TDto, T>
        where TDto : class
        where T : class
    {
        public SearchRequest<T> ConvertRequest(SearchRequest<TDto> searchRequest) =>
            new SearchRequest<T>
            {
                Filters = CreateFilters(searchRequest.Filters ?? Array.Empty<Filtering>()).ToArray(),
                Fields = searchRequest.Fields ?? Array.Empty<string>(),
                Sorts = CreateSorting(searchRequest.Sorts ?? Array.Empty<Sorting>()).ToArray(),
                Framing = CreateFraming(searchRequest.Framing ?? new Framing()),
                SkipCount = searchRequest.SkipCount,
            };

        private Framing CreateFraming(Framing searchRequestFraming) =>
            new Framing
            {
                Take = searchRequestFraming.Take,
                Skip = searchRequestFraming.Skip,
            };

        private IEnumerable<Sorting> CreateSorting(IEnumerable<Sorting> sortings)
        {
            foreach (var item in sortings)
            {
                // Start with incoming values
                var outer = (item.SortBy ?? "Id").Trim();
                var inner = item.InnerSortBy?.Trim();

                // If SortBy arrived as a dotted path (e.g., "Assignee.IdNumber"),
                // split it into outer="Assignee", inner="IdNumber".
                if (string.IsNullOrEmpty(inner) && outer.Contains('.'))
                {
                    var idx = outer.IndexOf('.');
                    inner = outer.Substring(idx + 1);
                    outer = outer.Substring(0, idx);
                }

                // We must be able to map the OUTER field name (top-level)
                if (CanMapField(outer, out var mappedOuter))
                {
                    // Inner is optional; map it if there’s a direct mapping, otherwise pass through
                    string? mappedInner = null;
                    if (!string.IsNullOrWhiteSpace(inner))
                    {
                        mappedInner = CanMapField(inner, out var tmp) ? tmp : inner;
                    }

                    yield return new Sorting
                    {
                        SortBy = mappedOuter,   // mapped outer
                        InnerSortBy = mappedInner,   // may be null or passthrough
                        InverseOrder = item.InverseOrder
                    };
                }
                // else: silently skip unknown outer sort field (keeps prior behavior pattern)
            }
        }

        private bool CanMapField(string sourceFieldName, out string destinationFieldName)
        {
            return FieldsMapping.TryGetValue(sourceFieldName.Trim(), out destinationFieldName);
        }

        private IEnumerable<Filtering> CreateFilters(IEnumerable<Filtering> filters)
        {
            foreach (var item in filters)
            {
                if (CanMapField(item.PropertyName, out var mappedFiledName))
                {
                    yield return new Filtering
                    {
                        PropertyName = mappedFiledName,
                        GroupingOperation = item.GroupingOperation,
                        FilterParameters = item.FilterParameters
                            .Select(p => new FilterParameter
                            {
                                GroupingOperation = p.GroupingOperation,
                                IsNegated = p.IsNegated,
                                Operation = p.Operation,
                                InnerPath = p.InnerPath,
                                Values = p.Values.Select(v => v?.Trim()).ToArray(),
                            }).ToArray(),
                    };
                }
            }
        }

        private static readonly Lazy<Dictionary<string, string>> Mappings =
            new Lazy<Dictionary<string, string>>(CreateAutomaticMappings, isThreadSafe: true);

        protected Dictionary<string, string> FieldsMapping => Mappings.Value;

        private static Dictionary<string, string> CreateAutomaticMappings()
        {
            var result = new Dictionary<string, string>();

            var sourceFields = GetTypeProperties(typeof(TDto));
            var destinationTypes = GetTypeProperties(typeof(T));

            sourceFields
                .Join(destinationTypes, s => s, d => d, (s, d) => s)
                .ToList()
                .ForEach(s => result.Add(s, s));

            return result;
        }

        private static string[] GetTypeProperties(Type type) =>
            type
                .GetProperties()
                .Where(c => !c.GetIndexParameters().Any())
                .Select(c => c.Name).ToArray();
    }
}
