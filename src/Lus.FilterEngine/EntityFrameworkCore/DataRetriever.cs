using Microsoft.EntityFrameworkCore;
using Lus.FilterEngine.Persistence;
using Lus.FilterEngine.QueryBuilder;

namespace Lus.FilterEngine.EntityFrameworkCore
{
    public abstract class DataRetriever<TProjection> : IDataRetriever<TProjection>
    {
        private readonly DbContext context;

        protected DataRetriever(DbContext context)
        {
            this.context = context;
        }

        public async Task<IEnumerable<TProjection>> RetrieveAsync(
            IQueryParams<TProjection> queryParams,
            CancellationToken cancellationToken = default)
        {
            var query = queryParams.Apply(CreateRetrieveQuery(this.context));
            return await RebuildQuery(query).ToArrayAsync(cancellationToken);
        }

        public async Task<int> CountAsync(
            IQueryParams<TProjection> queryParams,
            CancellationToken cancellationToken = default)
        {
            var query = queryParams.Apply(CreateRetrieveQuery(this.context), filterOnly: true);
            return await RebuildQuery(query).CountAsync(cancellationToken);
        }

        protected abstract IQueryable<TProjection> CreateRetrieveQuery(DbContext context);

        protected virtual IQueryable<TProjection> RebuildQuery(IQueryable<TProjection> query)
        {
            return query;
        }
    }
}
