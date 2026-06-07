using System.Linq.Expressions;

namespace Lus.FilterEngine.Common
{
    public interface ISearchRequestPredicateBuilder<T>
        where T : class
    {
        Expression<Func<T, bool>> Build(SearchRequest<T> request);
    }
}
