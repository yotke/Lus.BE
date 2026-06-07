namespace Lus.FilterEngine.QueryBuilder
{
    public interface ITakeable<T>
    {
        IQueryParams<T> Take(int num);
        IQueryParams<T> First();
    }
}
