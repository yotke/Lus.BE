using EasyCaching.Core;
using Lus.Application.Common.Builders;
using Lus.Application.Documents.Builder.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lus.Api.Tests.Builders
{
    public class BuilderSessionStoreBaseTests
    {
        [Fact]
        public async Task Schema_below_current_is_discarded_not_migrated()
        {
            var cache = new Mock<IEasyCachingProvider>();
            cache.Setup(c => c.GetAsync<DocumentBuildSession>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CacheValue<DocumentBuildSession>(null!, false));

            var store = new TestStore(cache.Object);
            var stale = new DocumentBuildSession { UserId = 1, SchemaVersion = 0 };
            var loaded = await store.GetAsync(1, _ => Task.FromResult<DocumentBuildSession?>(stale));
            Assert.Null(loaded);
        }

        [Fact]
        public async Task Current_schema_is_returned()
        {
            var cache = new Mock<IEasyCachingProvider>();
            cache.Setup(c => c.GetAsync<DocumentBuildSession>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CacheValue<DocumentBuildSession>(null!, false));
            cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<DocumentBuildSession>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var store = new TestStore(cache.Object);
            var current = new DocumentBuildSession
            {
                UserId = 1,
                SchemaVersion = DocumentBuildSession.CurrentSchemaVersion
            };
            var loaded = await store.GetAsync(1, _ => Task.FromResult<DocumentBuildSession?>(current));
            Assert.NotNull(loaded);
            Assert.Equal(DocumentBuildSession.CurrentSchemaVersion, loaded!.SchemaVersion);
        }

        private sealed class TestStore : BuilderSessionStoreBase<DocumentBuildSession>
        {
            public TestStore(IEasyCachingProvider cache)
                : base(
                    new Lus.Application.Common.Services.SelfHealingStore(cache),
                    cache,
                    "docbuild:",
                    TimeSpan.FromDays(7),
                    NullLogger<TestStore>.Instance)
            {
            }

            public Task<DocumentBuildSession?> GetAsync(
                int userId,
                Func<CancellationToken, Task<DocumentBuildSession?>> durable)
                => GetCoreAsync(userId, DocumentBuildSession.CurrentSchemaVersion, durable, CancellationToken.None);
        }
    }
}
