using Lus.FilterEngine.Common;
using Lus.FilterEngine.QueryBuilder;
using System.ComponentModel.DataAnnotations;
using Sorting = Lus.FilterEngine.Common.Sorting;

namespace Lus.FilterEngine
{
    public sealed class SearchRequest<T> : IValidatableObject where T : class
    {
        public SearchRequest()
        {
            Framing = new Framing();
            Sorts = new List<Sorting>();
            Filters = new List<Filtering>();
        }

        public ICollection<Filtering>? Filters { get; set; }

        public ICollection<Sorting>? Sorts { get; set; }

        public Framing? Framing { get; set; }

        public bool? SkipCount { get; set; }

        public ICollection<string>? Fields { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var result = SearchRequestValidator.Validate(this);

            if (!result.Any())
            {
                return new List<ValidationResult> { ValidationResult.Success };
            }

            return result.Select(r => new ValidationResult(r.Value, new[] { r.Key }));
        }
    }
}
