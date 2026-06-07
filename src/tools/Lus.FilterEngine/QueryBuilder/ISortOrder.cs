namespace Lus.FilterEngine.QueryBuilder
{
    public interface ISortOrder<T>
    {
        IOrdered<T> Asc();
        IOrdered<T> Desc();
    }
}
