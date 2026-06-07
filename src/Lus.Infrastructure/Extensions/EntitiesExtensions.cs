namespace Lus.Infrastructure.Extensions
{
    public static class EntitiesExtensions
    {
        public static IEnumerable<TEntity> EmptyIfNull<TEntity>(this IEnumerable<TEntity> entities)
            where TEntity : class => entities == null ? new List<TEntity>() : entities;

        public static IEnumerable<T> AsNotNull<T>(this IEnumerable<T> enumerable)
            => enumerable ?? Enumerable.Empty<T>();
    }
}
