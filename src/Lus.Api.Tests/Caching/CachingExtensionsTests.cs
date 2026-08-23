using EasyCaching.Core;
using Lus.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lus.Api.Tests.Caching
{
    public class CachingExtensionsTests
    {
        [Fact]
        public void AddCaching_with_default_provider_resolves_in_memory()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Caching:ProviderName"] = "default"
                })
                .Build();

            services.AddCaching(config);
            using var sp = services.BuildServiceProvider();
            var cache = sp.GetRequiredService<IEasyCachingProvider>();
            Assert.NotNull(cache);
            Assert.Equal("default", cache.Name);
        }

        [Fact]
        public void AddCaching_with_redis_provider_registers_without_throwing()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Caching:ProviderName"] = "redis",
                    ["Redis:Host"] = "127.0.0.1",
                    ["Redis:Port"] = "6379",
                    ["Redis:Username"] = "",
                    ["Redis:Password"] = "",
                    ["Redis:Ssl"] = "false"
                })
                .Build();

            var ex = Record.Exception(() => services.AddCaching(config));
            Assert.Null(ex);
        }
    }
}
