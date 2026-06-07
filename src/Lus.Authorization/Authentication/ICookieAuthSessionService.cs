using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Lus.Authorization.Authentication
{
    public interface ICookieAuthSessionService
    {
        AuthenticationProperties CreateDefaultAuthenticationProperties(TimeSpan? lifetime = null);

        Task SignInAsync(
            HttpContext httpContext,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties = null);

        Task SignOutAsync(
            HttpContext httpContext,
            string? userId = null,
            string? organizationCacheKeyPrefix = null,
            CancellationToken cancellationToken = default);
    }
}
