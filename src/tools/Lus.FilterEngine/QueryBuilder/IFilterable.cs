using System.Linq.Expressions;

namespace Lus.FilterEngine.QueryBuilder
{
    public interface IFilterable<T> : ISelectable<T>
    {
        IFilterable<T> Where(Expression<Func<T, bool>> predicate);
    }
}
