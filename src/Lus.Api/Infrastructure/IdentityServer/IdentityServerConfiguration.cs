using IdentityServer4.Models;

namespace Lus.Infrastructure.IdentityServer
{
    public static class IdentityServerConfiguration
    {
        public static IEnumerable<IdentityResource> GetIdentityResources() =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource("userroles", new[] { "userrole" })
            };

        public static IEnumerable<ApiResource> GetApiResources(IConfiguration configuration)
        {
            var rawResources = configuration.GetSection("IdentityServer:ApiResources").Get<List<ApiResource>>();

            return rawResources;
        }

        public static IEnumerable<ApiScope> GetApiScopes(IConfiguration configuration)
        {
            var rawScopes = configuration.GetSection("IdentityServer:ApiScopes").Get<List<ApiScope>>();

            return rawScopes;
        }
    }
}
