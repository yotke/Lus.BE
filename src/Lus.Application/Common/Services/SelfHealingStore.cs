using EasyCaching.Core;
using Microsoft.Extensions.Logging;

namespace Lus.Application.Common.Services
{
    /// <summary>
    /// Redis is the FAST tier; a durable loader/saver pair is the TRUTH tier.
    /// Cache outages degrade to durable-only — they must never lose user data.
    /// </summary>
    public interface ISelfHealingStore
    {
        Task<T?> GetAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<T?>> durableLoad,
            TimeSpan ttl,
            CancellationToken ct) where T : class;

        Task SetAsync<T>(
            string cacheKey,
            T value,
            Func<T, CancellationToken, Task> durableSave,
            TimeSpan ttl,
            CancellationToken ct) where T : class;
    }

    public class SelfHealingStore : ISelfHealingStore
    {
        private readonly IEasyCachingProvider provider;
        private readonly ILogger<SelfHealingStore>? logger;

        public SelfHealingStore(IEasyCachingProvider provider, ILogger<SelfHealingStore>? logger = null)
        {
            this.provider = provider;
            this.logger = logger;
        }

        public async Task<T?> GetAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<T?>> durableLoad,
            TimeSpan ttl,
            CancellationToken ct) where T : class
        {
            try
            {
                var cached = await this.provider.GetAsync<T>(cacheKey, ct);
                if (cached.HasValue && cached.Value != null) return cached.Value;
            }
            catch (Exception cacheEx)
            {
                this.logger?.LogWarning(cacheEx, "SelfHealingStore: cache read failed for {Key} — durable fallback.", cacheKey);
            }

            var value = await durableLoad(ct);
            if (value == null) return null;

            try
            {
                await this.provider.SetAsync(cacheKey, value, ttl, ct);
            }
            catch (Exception setEx)
            {
                this.logger?.LogWarning(setEx, "SelfHealingStore: cache heal failed for {Key}.", cacheKey);
            }
            return value;
        }

        public async Task SetAsync<T>(
            string cacheKey,
            T value,
            Func<T, CancellationToken, Task> durableSave,
            TimeSpan ttl,
            CancellationToken ct) where T : class
        {
            await durableSave(value, ct);
            try
            {
                await this.provider.SetAsync(cacheKey, value, ttl, ct);
            }
            catch (Exception setEx)
            {
                this.logger?.LogWarning(setEx, "SelfHealingStore: cache write failed for {Key} (durable saved).", cacheKey);
            }
        }
    }
}
