using IdentityServer4.Models;
using IdentityServer4.Services;
using Lus.Application.Common.Services;
using Lus.Infrastructure.IdentityServer;

namespace Lus.Infrastructure.Extensions
{
    public static class CustomClientStoreCacheExtensions
    {
        public static IIdentityServerBuilder AddCustomClientStoreCache(this IIdentityServerBuilder identityServerBuilder)
        {
            identityServerBuilder.Services.AddTransient<CustomClientCache<Client>>()
                .AddTransient<ICache<Client>>(sp => sp.GetRequiredService<CustomClientCache<Client>>())
                .AddTransient<IClientCacheWithReset>(sp => sp.GetRequiredService<CustomClientCache<Client>>());

            identityServerBuilder.AddClientStoreCache<DatabaseClientsStore>()
                .AddInMemoryCaching();

            return identityServerBuilder;
        }
    }
}
