using Lus.FilterEngine.QueryBuilder;

namespace Lus.FilterEngine.Persistence
{
    public interface IDataRetriever<TProjection>
    {
        Task<IEnumerable<TProjection>> RetrieveAsync(IQueryParams<TProjection> queryParams, CancellationToken cancellationToken = default);
        Task<int> CountAsync(IQueryParams<TProjection> queryParams, CancellationToken cancellationToken = default);
    }
}
