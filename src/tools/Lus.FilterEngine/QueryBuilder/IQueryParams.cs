namespace Lus.FilterEngine.QueryBuilder
{
    public interface IQueryParams<T>
    {
        IQueryable<T> Apply(IQueryable<T> query, bool filterOnly = false);
    }
}
