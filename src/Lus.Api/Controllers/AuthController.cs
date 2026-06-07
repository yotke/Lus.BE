using System.Security.Claims;
using EasyCaching.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lus.Application;
using Lus.Application.Users.Queries.GetAuthUserInfo;
using Lus.Authorization.Authentication;
using Lus.Contracts.Organizations;
using Lus.Contracts.Users;

namespace Lus.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly ICookieAuthSessionService cookieAuthSessionService;
        private readonly IEasyCachingProvider cacheProvider;

        public AuthController(
            IMediator mediator,
            ICookieAuthSessionService cookieAuthSessionService,
            IEasyCachingProvider cacheProvider)
        {
            this.mediator = mediator;
            this.cookieAuthSessionService = cookieAuthSessionService;
            this.cacheProvider = cacheProvider;
        }

        [HttpGet("check")]
        [AllowAnonymous]
        public IActionResult Check()
        {
            return Ok(new { isAuthenticated = User?.Identity?.IsAuthenticated == true });
        }

        [HttpGet("state")]
        [AllowAnonymous]
        public async Task<IActionResult> State(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Ok(new { isAuthenticated = false });
            }

            var user = await this.mediator.Send(new GetAuthUserInfoQuery(userId.Value), cancellationToken);
            if (user == null)
            {
                return Ok(new { isAuthenticated = false });
            }

            var currentOrganization = await ResolveCurrentOrganizationAsync(user, cancellationToken);
            return Ok(new
            {
                isAuthenticated = true,
                user = new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    phone = user.Phone,
                    idNumber = user.IdNumber
                },
                currentOrganization,
                organizations = user.Organizations ?? Array.Empty<AuthOrganizationDto>(),
                roles = user.Roles ?? Array.Empty<Lus.Contracts.Roles.AuthRoleDto>(),
                permissions = user.Claims?
                    .Where(c => c.Key == ApplicationConstants.ClaimsTypes.Permission)
                    .Select(c => c.Value)
                    .ToArray() ?? Array.Empty<string>()
            });
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = CookieAuthSchemes.Api)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = GetUserId()?.ToString();
            await this.cookieAuthSessionService.SignOutAsync(
                HttpContext,
                userId,
                ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey,
                cancellationToken);

            return NoContent();
        }

        private int? GetUserId()
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sub")?.Value;

            return int.TryParse(value, out var userId) && userId > 0 ? userId : null;
        }

        private async Task<AuthOrganizationDto?> ResolveCurrentOrganizationAsync(
            AuthUserInfo user,
            CancellationToken cancellationToken)
        {
            var key = $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{user.Id}";
            var cached = await this.cacheProvider.GetAsync<int?>(key, cancellationToken);
            var organizationId = cached.HasValue ? cached.Value : user.Organizations?.FirstOrDefault()?.Id;
            return user.Organizations?.FirstOrDefault(o => o.Id == organizationId)
                ?? user.Organizations?.FirstOrDefault();
        }
    }
}
