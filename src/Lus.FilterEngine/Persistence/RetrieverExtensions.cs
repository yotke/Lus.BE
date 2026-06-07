using Lus.FilterEngine.QueryBuilder;

namespace Lus.FilterEngine.Persistence
{
    public static class RetrieverExtensions
    {
        public static async Task<DataFrame<T>> RetrieveFrameUsingAsync<T>(this IQueryParams<T> queryParams, IDataRetriever<T> retriver,
            bool skipCount = false, CancellationToken cancellationToken = default)
        {
            var frame = await retriver.RetrieveAsync(queryParams, cancellationToken);
            int? count = null;
            if (!skipCount)
            {
                count = await retriver.CountAsync(queryParams, cancellationToken);
            }

            return new DataFrame<T>
            {
                Items = frame,
                Count = count ?? 0
            };
        }

        public static async Task<IEnumerable<T>> RetrieveUsingAsync<T>(this IQueryParams<T> queryParams, IDataRetriever<T> retriver,
            CancellationToken cancellationToken = default)
        {
            var frame = await retriver.RetrieveAsync(queryParams, cancellationToken);
            return frame;
        }
    }
}
