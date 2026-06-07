using System.Linq.Expressions;

namespace Lus.FilterEngine.QueryBuilder
{
    public static class Items<T>
    {
        public static IFilterable<T> Where(Expression<Func<T, bool>> predicate)
        {
            IFilterable<T> context = new FramedQueryContext<T>();
            return context.Where(predicate);
        }

        public static ISortOrder<T> SortedBy<TType>(Expression<Func<T, TType>> sortBy)
        {
            ISortable<T> context = new FramedQueryContext<T>();
            return context.SortedBy(sortBy);
        }
    }
}
