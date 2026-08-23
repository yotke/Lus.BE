using EasyCaching.Core;
using Lus.Application.Common.Builders;
using Lus.Application.Common.Services;
using Microsoft.Extensions.Logging;

namespace Lus.Application.Documents.Builder.Services
{
    public class DocumentBuildSessionStore : BuilderSessionStoreBase<DocumentBuildSession>
    {
        public static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

        public DocumentBuildSessionStore(
            ISelfHealingStore store,
            IEasyCachingProvider cache,
            ILogger<DocumentBuildSessionStore>? logger = null)
            : base(store, cache, "docbuild:", Ttl, logger)
        {
        }

        public Task<DocumentBuildSession?> GetAsync(int userId, CancellationToken ct) =>
            GetCoreAsync(
                userId,
                DocumentBuildSession.CurrentSchemaVersion,
                _ => Task.FromResult<DocumentBuildSession?>(null),
                ct);

        public Task SaveAsync(DocumentBuildSession session, CancellationToken ct) =>
            SaveCoreAsync(session, (_, _) => Task.CompletedTask, ct);

        public async Task<DocumentBuildSession> GetOrCreateAsync(int userId, CancellationToken ct)
        {
            var existing = await GetAsync(userId, ct);
            if (existing is not null) return existing;
            return new DocumentBuildSession
            {
                UserId = userId,
                SchemaVersion = DocumentBuildSession.CurrentSchemaVersion,
                Draft = new() { Version = 0 }
            };
        }
    }
}
