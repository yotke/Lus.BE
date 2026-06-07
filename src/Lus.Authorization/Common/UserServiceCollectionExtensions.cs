using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EasyCaching.Core;
using Lus.Authorization.Extensions;
using Lus.Authorization.Models;

namespace Lus.Authorization.Common
{
    public static class UserServiceCollectionExtensions
    {
        private const string TokenEndpoint = "/connect/token";
        private const string IdentityTokenClientName = "LusClient";

        public static IServiceCollection AddUserAccessor(this IServiceCollection services,
            IProjectUser projectUser)
        {
            services.AddHttpContextAccessor();

            services.TryAddSingleton(provider =>
            {
                var userAccessorResolver =
                    provider.GetService<IUserAccessorFactory>() ?? new DefaultUserAccessorFactory();
                return userAccessorResolver.CreateUserAccessor(provider, projectUser);
            });

            return services;
        }

        public static IServiceCollection AddUserAccessor<TUserAccessorFactory>(
            this IServiceCollection services,
            ServiceClientConfiguration identityConfiguration,
            Action<IHttpClientBuilder> httpClientSetup = null,
            Action<AllowedClaimTypeOptions> configureAction = null)
            where TUserAccessorFactory : class, IUserAccessorFactory
        {
            services.Configure<AllowedClaimTypeOptions>(o => configureAction?.Invoke(o));
            services.TryAddTransient<IUserAccessorFactory, TUserAccessorFactory>();
            services.AddUserAccessor(identityConfiguration, httpClientSetup);
            return services;
        }

        public static IServiceCollection AddUserAccessor(
            this IServiceCollection services,
            ServiceClientConfiguration identityConfiguration,
            Action<IHttpClientBuilder> httpClientSetup = null)
        {
            var applicationUser = GetCurrentUser(services, identityConfiguration, httpClientSetup);
            return AddUserAccessor(services, applicationUser);
        }

        private static IProjectUser GetCurrentUser(IServiceCollection services,
            ServiceClientConfiguration identityConfiguration, Action<IHttpClientBuilder> httpClientSetup)
        {
            var httpClientBuilder = services.AddHttpClient(IdentityTokenClientName);

            httpClientSetup?.Invoke(httpClientBuilder);

            using (var provider = services.BuildServiceProvider())
            {
                if (identityConfiguration == null)
                {
                    throw new Exception($"{nameof(IdentityApiOptions)}  is not configured.");
                }

                var cacheProvider = provider.GetRequiredService<IEasyCachingProvider>();

                var identityClient = provider.GetRequiredService<IHttpClientFactory>()
                            .CreateClient(IdentityTokenClientName);
                var identityResponse = identityClient.RequestTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = identityConfiguration.Url.ToString().TrimEnd('/') + TokenEndpoint,
                    ClientId = identityConfiguration.ClientId,
                    ClientSecret = identityConfiguration.ClientSecret,
                    Scope = identityConfiguration.ApiScope,
                    GrantType = OidcConstants.GrantTypes.ClientCredentials,
                })
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                if (identityResponse.IsError)
                {
                    return new ProjectUser { };
                }

                JwtSecurityToken token;

                try
                {
                    token = new JwtSecurityToken(identityResponse.AccessToken);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error during Identity Token parse on startup.", e);
                }

                var userIdClaim =
                    token.Claims.FirstOrDefault(c => c.Type.Equals("sub", StringComparison.OrdinalIgnoreCase));

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    throw new Exception("Application token on startup doesn't contain User Id.");
                }

                var organizationId = cacheProvider.Get<int?>($"user_organization_id_{userId}").HasValue
                        ? cacheProvider.Get<int?>($"user_organization_id_{userId}").Value
                        : null;

                return new ProjectUser
                {
                    Id = userId,
                    Name = token.Claims.FirstOrDefault(c => c.Type.Equals("name", StringComparison.OrdinalIgnoreCase))
                        ?.Value ?? token.Claims.GetClaimValue<string>(ClaimTypes.Name),
                    OrganizationId = organizationId,
                    Roles = new[] { token.Claims.GetClaimValue<string>(ClaimTypes.Role) },
                    Claims = new[]
                    {
                        (CustomClaimTypes.Permission, token.Claims.GetClaimValue<string>(CustomClaimTypes.Permission))
                    },
                };
            }
        }
    }
}
