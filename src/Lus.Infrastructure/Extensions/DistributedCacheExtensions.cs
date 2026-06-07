using Microsoft.Extensions.Caching.Distributed;

namespace Lus.Infrastructure.Extensions
{
    public static class DistributedCacheExtensions
    {
        public static async Task<TModel> GetModelByKeyAsync<TModel>(this IDistributedCache cache, string key)
        {
            var model = await cache.GetStringAsync(key);
            return model.AsModel<TModel>();
        }

        public static async Task StoreModelAsync<TModel>(this IDistributedCache cache, string key, TModel model, int minutesToExpire)
        {
            if (model == null)
            {
                return;
            }

            await cache.SetStringAsync(key, model.ToModelString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutesToExpire)
            });
        }
    }
}
