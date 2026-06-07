namespace Lus.FilterEngine.QueryBuilder
{
    public interface ISelectable<T> : ISortable<T>
    {
        ISelectable<T> Select(string[] fields);
    }
}
