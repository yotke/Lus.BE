using System.Security.Claims;
using EasyCaching.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Lus.Authorization.Authentication
{
    public sealed class CookieAuthSessionService : ICookieAuthSessionService
    {
        private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);

        public AuthenticationProperties CreateDefaultAuthenticationProperties(TimeSpan? lifetime = null)
        {
            return new AuthenticationProperties
            {
                IsPersistent = true,
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(lifetime ?? DefaultLifetime),
                AllowRefresh = true
            };
        }

        public async Task SignInAsync(
            HttpContext httpContext,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties = null)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            ArgumentNullException.ThrowIfNull(principal);

            var authProperties = properties ?? CreateDefaultAuthenticationProperties();
            await httpContext.SignInAsync(CookieAuthSchemes.IdentityServer, principal, authProperties);
            await httpContext.SignInAsync(CookieAuthSchemes.Api, principal, authProperties);
        }

        public async Task SignOutAsync(
            HttpContext httpContext,
            string? userId = null,
            string? organizationCacheKeyPrefix = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            await httpContext.SignOutAsync(CookieAuthSchemes.Api);
            await httpContext.SignOutAsync(CookieAuthSchemes.IdentityServer);
            httpContext.Session?.Clear();

            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(organizationCacheKeyPrefix))
            {
                var cache = httpContext.RequestServices.GetService(typeof(IEasyCachingProvider)) as IEasyCachingProvider;
                if (cache != null)
                {
                    await cache.RemoveAsync($"{organizationCacheKeyPrefix}{userId}", cancellationToken);
                }
            }
        }
    }
}
