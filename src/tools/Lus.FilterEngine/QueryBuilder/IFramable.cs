namespace Lus.FilterEngine.QueryBuilder
{
    public interface IFramable<T> : ITakeable<T>, IQueryParams<T>
    {
        IQueryParams<T> All();
        ITakeable<T> Skip(int num);
    }
}
