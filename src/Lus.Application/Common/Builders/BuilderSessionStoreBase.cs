using Lus.Application.Common.Services;
using EasyCaching.Core;
using Microsoft.Extensions.Logging;

namespace Lus.Application.Common.Builders
{
    /// <summary>Minimal shape a builder session payload must expose for the store base to work.</summary>
    public interface IBuilderSession
    {
        int UserId { get; }
        int SchemaVersion { get; }
    }

    /// <summary>
    /// Redis-fast-tier + DB-truth-tier self-healing session store (stability-plan L2),
    /// extracted bottom-up (ARCH-1) from Organizations/Builder/Services/OrgBuildSessionStore
    /// once the Rules builder needed the identical pattern. A subclass owns its own row
    /// type/repository and supplies durable load/save/delete delegates; this base wires them
    /// through <see cref="ISelfHealingStore"/> with: cache key "{prefix}{userId}", the TTL
    /// re-set on every save (so the absolute expiry behaves as sliding), SchemaVersion
    /// discard-not-migrate on load (pre-GA policy — no migration), durable-first save then
    /// cache heal, and fail-soft cache eviction on delete (a cache outage must never block a
    /// reset — the durable row is already gone).
    /// </summary>
    public abstract class BuilderSessionStoreBase<TSession> where TSession : class, IBuilderSession
    {
        private readonly ISelfHealingStore store;
        private readonly IEasyCachingProvider cache;
        private readonly string cacheKeyPrefix;
        private readonly TimeSpan ttl;

        protected ILogger? Logger { get; }

        protected BuilderSessionStoreBase(
            ISelfHealingStore store,
            IEasyCachingProvider cache,
            string cacheKeyPrefix,
            TimeSpan ttl,
            ILogger? logger = null)
        {
            this.store = store;
            this.cache = cache;
            this.cacheKeyPrefix = cacheKeyPrefix;
            this.ttl = ttl;
            this.Logger = logger;
        }

        protected string CacheKey(int userId) => $"{this.cacheKeyPrefix}{userId}";

        /// <summary>
        /// Current session for the user (Redis fast tier → DB rescue via <paramref name="durableLoad"/>),
        /// or null. A rescued payload whose SchemaVersion is below <paramref name="currentSchemaVersion"/>
        /// is discarded, not migrated.
        /// </summary>
        protected Task<TSession?> GetCoreAsync(
            int userId, int currentSchemaVersion,
            Func<CancellationToken, Task<TSession?>> durableLoad, CancellationToken ct)
            => this.store.GetAsync<TSession>(
                CacheKey(userId),
                durableLoad: async token =>
                {
                    var session = await durableLoad(token);
                    if (session is null) return null;

                    if (session.SchemaVersion < currentSchemaVersion)
                    {
                        this.Logger?.LogWarning(
                            "{Type} for user {UserId} has schema {Schema} < {Current} — discarding.",
                            typeof(TSession).Name, userId, session.SchemaVersion, currentSchemaVersion);
                        return null;
                    }

                    return session;
                },
                this.ttl,
                ct);

        /// <summary>Durable-first save (repository upsert via <paramref name="durableSave"/>), then cache heal. Refreshes the TTL.</summary>
        protected Task SaveCoreAsync(
            TSession session, Func<TSession, CancellationToken, Task> durableSave, CancellationToken ct)
            => this.store.SetAsync(CacheKey(session.UserId), session, durableSave, this.ttl, ct);

        /// <summary>Hard-deletes the durable row via <paramref name="durableDelete"/>, then fail-soft evicts the cache key.</summary>
        protected async Task DeleteCoreAsync(
            int userId, Func<CancellationToken, Task> durableDelete, CancellationToken ct)
        {
            await durableDelete(ct);

            try
            {
                await this.cache.RemoveAsync(CacheKey(userId), ct);
            }
            catch (Exception ex)
            {
                // Fail-soft like SelfHealingStore: a cache outage must not block the reset;
                // the durable row is already gone, so a stale cache entry dies with its TTL.
                this.Logger?.LogWarning(ex,
                    "{StoreType}: cache evict failed for user {UserId} (row deleted).",
                    this.GetType().Name, userId);
            }
        }
    }
}
