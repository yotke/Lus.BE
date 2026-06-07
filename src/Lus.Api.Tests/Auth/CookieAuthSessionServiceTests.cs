using System.Security.Claims;
using Lus.Authorization.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lus.Api.Tests.Auth;

public class CookieAuthSessionServiceTests
{
    [Fact]
    public async Task SignInAsync_Issues_Api_And_IdentityServer_Cookies()
    {
        var services = new ServiceCollection();
        var authService = new RecordingAuthenticationService();
        services.AddSingleton<IAuthenticationService>(authService);
        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "42") },
            CookieAuthSchemes.Api));

        await new CookieAuthSessionService().SignInAsync(context, principal);

        Assert.Contains(CookieAuthSchemes.Api, authService.SignedInSchemes);
        Assert.Contains(CookieAuthSchemes.IdentityServer, authService.SignedInSchemes);
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public List<string> SignedInSchemes { get; } = new();

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignedInSchemes.Add(scheme ?? string.Empty);
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }
    }
}
