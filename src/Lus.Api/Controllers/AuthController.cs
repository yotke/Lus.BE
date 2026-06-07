using System.Security.Claims;
using EasyCaching.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lus.Application;
using Lus.Application.Common.Ports;
using Lus.Application.Common.Services;
using Lus.Application.Users.Commands.ConfirmUser;
using Lus.Application.Users.Commands.LoginUserByToken;
using Lus.Application.Users.Queries.GetAuthUserInfo;
using Lus.Authorization.Authentication;
using Lus.Contracts.Auth;
using Lus.Contracts.Organizations;
using Lus.Contracts.Users;
using Lus.Contracts.Users.Types;

namespace Lus.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly ICookieAuthSessionService cookieAuthSessionService;
        private readonly IEasyCachingProvider cacheProvider;
        private readonly IUsersService usersService;
        private readonly IRecaptchaAdapter recaptchaAdapter;

        public AuthController(
            IMediator mediator,
            ICookieAuthSessionService cookieAuthSessionService,
            IEasyCachingProvider cacheProvider,
            IUsersService usersService,
            IRecaptchaAdapter recaptchaAdapter)
        {
            this.mediator = mediator;
            this.cookieAuthSessionService = cookieAuthSessionService;
            this.cacheProvider = cacheProvider;
            this.usersService = usersService;
            this.recaptchaAdapter = recaptchaAdapter;
        }

        /// <summary>
        /// Password login. Replaces the IdentityServer4 resource-owner-password grant.
        /// On success an HttpOnly auth cookie is issued.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Ok(LoginResponseDto.Failure(10));
            }

            if (!await this.recaptchaAdapter.CheckRecaptcha(cancellationToken))
            {
                return Ok(LoginResponseDto.Failure(41));
            }

            var email = request.Email.ToLowerInvariant();
            var loginResult = await this.usersService.LoginAsync(email, request.Password);

            var failure = MapLoginFailure(loginResult);
            if (failure != null)
            {
                return Ok(failure);
            }

            return Ok(await SignInByEmailAsync(email, cancellationToken));
        }

        /// <summary>
        /// Confirms a user via the e-mail confirmation token and signs them in.
        /// Replaces the IdentityServer4 "confirm_token" extension grant.
        /// </summary>
        [HttpPost("confirm-token")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        public async Task<IActionResult> ConfirmToken(ConfirmTokenRequestDto request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ConfirmToken))
            {
                return Ok(LoginResponseDto.Failure(20));
            }

            if (!await this.recaptchaAdapter.CheckRecaptcha(cancellationToken))
            {
                return Ok(LoginResponseDto.Failure(41));
            }

            var user = await this.mediator.Send(new ConfirmUserCommand(request.ConfirmToken), cancellationToken);
            if (user == null)
            {
                return Ok(LoginResponseDto.Failure(20));
            }

            return Ok(await SignInByUserIdAsync(user.Id, cancellationToken));
        }

        /// <summary>
        /// One-time SMS-code login. Replaces the IdentityServer4 "login_by_token" extension grant.
        /// </summary>
        [HttpPost("login-by-sms")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        public async Task<IActionResult> LoginBySms(SmsLoginRequestDto request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SmsCode))
            {
                return Ok(LoginResponseDto.Failure(20));
            }

            if (!await this.recaptchaAdapter.CheckRecaptcha(cancellationToken))
            {
                return Ok(LoginResponseDto.Failure(41));
            }

            var lockedResult = await this.usersService.IsAccountLockedAsync(request.SmsCode);
            if (!lockedResult.IsUserFound)
            {
                return Ok(LoginResponseDto.Failure(20));
            }

            if (lockedResult.IsLocked)
            {
                return Ok(LoginResponseDto.Locked(101, lockedResult.LockTimeLeft));
            }

            var user = await this.mediator.Send(new LoginUserByTokenCommand(request.SmsCode), cancellationToken);
            if (user == null)
            {
                return Ok(LoginResponseDto.Failure(22));
            }

            if (!user.IsConfirmed)
            {
                return Ok(LoginResponseDto.Failure(11));
            }

            return Ok(await SignInByUserIdAsync(user.Id, cancellationToken));
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

        private async Task<LoginResponseDto> SignInByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var user = await this.mediator.Send(new GetAuthUserInfoQuery(email), cancellationToken);
            return await SignInAsync(user);
        }

        private async Task<LoginResponseDto> SignInByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            var user = await this.mediator.Send(new GetAuthUserInfoQuery(userId), cancellationToken);
            return await SignInAsync(user);
        }

        private async Task<LoginResponseDto> SignInAsync(AuthUserInfo user)
        {
            if (user == null)
            {
                return LoginResponseDto.Failure(10);
            }

            var principal = CookiePrincipalFactory.Create(user, CookieAuthSchemes.Api);
            await this.cookieAuthSessionService.SignInAsync(HttpContext, principal);

            return new LoginResponseDto
            {
                IsSuccess = true,
                User = new AuthenticatedUserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    IdNumber = user.IdNumber,
                    Roles = user.Roles,
                    Organizations = user.Organizations
                }
            };
        }

        private static LoginResponseDto MapLoginFailure(UserLoginResult loginResult)
        {
            if (loginResult.LoginFailReason == null)
            {
                return loginResult.IsValidCredentials ? null : LoginResponseDto.Failure(10);
            }

            return loginResult.LoginFailReason switch
            {
                LoginFailReasonType.UserNotFound => LoginResponseDto.Failure(10),
                LoginFailReasonType.WrongPassword => LoginResponseDto.Failure(12),
                LoginFailReasonType.UserNotConfirm => LoginResponseDto.Failure(11),
                LoginFailReasonType.PasswordExpired => LoginResponseDto.Failure(13, loginResult.ErrorMessage),
                LoginFailReasonType.BlockedUser => LoginResponseDto.Locked(101, loginResult.LockTimeLeft),
                _ => LoginResponseDto.Failure(10)
            };
        }

        private int? GetUserId()
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sub")?.Value;

            return int.TryParse(value, out var userId) && userId > 0 ? userId : null;
        }

        private async Task<AuthOrganizationDto> ResolveCurrentOrganizationAsync(
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

