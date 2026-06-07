using Lus.FilterEngine;
using Lus.FilterEngine.Builder;
using Lus.FilterEngine.Common;
using Lus.FilterEngine.Operations;
using Xunit;

namespace Lus.Api.Tests.Search
{
    /// <summary>
    /// Exercises the expression-tree predicate builder directly over in-memory data
    /// (no DB) to validate operator translation.
    /// </summary>
    public class FilterEnginePredicateTests
    {
        private sealed class Row
        {
            public int OrganizationId { get; set; }
            public string? Name { get; set; }
        }

        private static Filtering Filter(string property, FilterOperation op, string value) => new Filtering
        {
            PropertyName = property,
            GroupingOperation = BooleanOperation.And,
            FilterParameters = new List<FilterParameter>
            {
                new FilterParameter { Operation = op, Values = new[] { value }, GroupingOperation = BooleanOperation.Or }
            }
        };

        [Fact]
        public void Eq_filter_matches_exact_value()
        {
            var request = new SearchRequest<Row>
            {
                Filters = new List<Filtering> { Filter(nameof(Row.OrganizationId), FilterOperation.Eq, "5") }
            };

            var predicate = new SearchRequestPredicateBuilder<Row>().Build(request).Compile();
            var rows = new[]
            {
                new Row { OrganizationId = 5, Name = "a" },
                new Row { OrganizationId = 6, Name = "b" }
            };

            var matched = rows.Where(predicate).ToList();

            Assert.Single(matched);
            Assert.Equal(5, matched[0].OrganizationId);
        }

        [Fact]
        public void Contains_filter_matches_substring()
        {
            var request = new SearchRequest<Row>
            {
                Filters = new List<Filtering> { Filter(nameof(Row.Name), FilterOperation.Ct, "ell") }
            };

            var predicate = new SearchRequestPredicateBuilder<Row>().Build(request).Compile();
            var rows = new[]
            {
                new Row { Name = "hello" },
                new Row { Name = "world" }
            };

            var matched = rows.Where(predicate).ToList();

            Assert.Single(matched);
            Assert.Equal("hello", matched[0].Name);
        }

        [Fact]
        public void No_filters_matches_everything()
        {
            var request = new SearchRequest<Row> { Filters = new List<Filtering>() };

            var predicate = new SearchRequestPredicateBuilder<Row>().Build(request).Compile();
            var rows = new[] { new Row { OrganizationId = 1 }, new Row { OrganizationId = 2 } };

            Assert.Equal(2, rows.Where(predicate).Count());
        }
    }
}
