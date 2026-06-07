using Lus.FilterEngine.Operations;
using Lus.FilterEngine.QueryBuilder;

namespace Lus.FilterEngine.Common
{
    public sealed class SearchRequestBuilder<T> where T : class
    {
        private readonly List<Filtering> filters = new List<Filtering>();
        private readonly List<Sorting> sorts = new List<Sorting>();
        private readonly List<string> fields = new List<string>();
        private Framing framing = new Framing { Skip = 0, Take = 10 };
        private bool skipCount = false;

        private SearchRequestBuilder() { }

        public static SearchRequestBuilder<T> Empty => new SearchRequestBuilder<T>();

        public SearchRequestBuilder<T> AppendFiltering(string propertyName, string value) =>
            AppendFiltering(propertyName, FilterOperation.Eq, value);

        public SearchRequestBuilder<T> AppendFiltering(string propertyName, FilterOperation filterOperation, string value) =>
            AppendFiltering(propertyName, filterOperation, BooleanOperation.And, value);

        public SearchRequestBuilder<T> AppendFiltering(string propertyName, FilterOperation filterOperation, params string[] values) =>
            AppendFiltering(propertyName, filterOperation, BooleanOperation.And, values);

        /// <summary>
        /// Obsolete reason: Boolean operation cannot be applied to single filter parameter in FilterParameters.
        /// See class SearchRequestPredicateBuilder, method CreateFieldFilterExpression, rows 73-77
        /// </summary>
        [Obsolete]
        public SearchRequestBuilder<T> AppendFiltering(string propertyName, BooleanOperation booleanOperation, FilterOperation filterOperation, params string[] values)
        {
            this.filters.Add(new Filtering
            {
                PropertyName = propertyName,
                FilterParameters = new List<FilterParameter>
                {
                    new FilterParameter
                    {
                        GroupingOperation = booleanOperation,
                        Operation = filterOperation,
                        Values = values,
                    },
                },
            });

            return this;
        }

        /// <summary>
        /// Appends filter with logical operation on filter level.
        /// </summary>
        public SearchRequestBuilder<T> AppendFiltering(
            string propertyName,
            FilterOperation filterOperation,
            BooleanOperation filterGroupingOperation,
            params string[] values)
        {
            this.filters.Add(new Filtering
            {
                GroupingOperation = filterGroupingOperation,
                PropertyName = propertyName,
                FilterParameters = new List<FilterParameter>
                {
                    new FilterParameter
                    {
                        Operation = filterOperation,
                        Values = values,
                    },
                },
            });

            return this;
        }

        public SearchRequestBuilder<T> AppendSorting(string propertyName) =>
            AppendSorting(propertyName, false);

        public SearchRequestBuilder<T> AppendSorting(string propertyName, bool isDescending)
        {
            this.sorts.Add(new Sorting
            {
                SortBy = propertyName,
                InverseOrder = isDescending,
            });

            return this;
        }

        public SearchRequestBuilder<T> SetFraming(int take) =>
            SetFraming(take, 0);

        public SearchRequestBuilder<T> SetFraming(int take, int skip)
        {
            this.framing = new Framing
            {
                Skip = skip,
                Take = take,
            };

            return this;
        }

        public SearchRequestBuilder<T> WithNoCount()
        {
            this.skipCount = true;
            return this;
        }

        public SearchRequestBuilder<T> WithFields(params string[] fieldsToRetrieve)
        {
            this.fields.AddRange(fieldsToRetrieve);
            return this;
        }

        public SearchRequest<T> Build() =>
            new SearchRequest<T>
            {
                Filters = this.filters,
                Fields = this.fields,
                Sorts = this.sorts,
                Framing = this.framing,
                SkipCount = this.skipCount,
            };
    }
}
