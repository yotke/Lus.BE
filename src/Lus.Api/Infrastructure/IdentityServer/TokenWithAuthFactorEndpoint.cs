using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Lus.Application;
using Lus.Application.Users.Queries.GetAuthUserInfo;
using Lus.Authorization.Authentication;
using Lus.Contracts.Users;
using Lus.Infrastructure.IdentityServer.Models;
using System.Security.Claims;

namespace Lus.Infrastructure.IdentityServer
{
    public class TokenWithAuthFactorEndpoint : IEndpointHandler
    {
        private readonly IClientSecretValidator clientValidator;
        private readonly ITokenRequestValidator requestValidator;
        private readonly IEventService events;
        private readonly ILogger<TokenWithAuthFactorEndpoint> logger;
        private readonly ISystemClock systemClock;
        private readonly ITokenCreationService tokenCreationService;
        private readonly ITokenResponseGenerator responseGenerator;
        private readonly IMediator mediator;
        private readonly ICookieAuthSessionService cookieAuthSessionService;

        public TokenWithAuthFactorEndpoint(
            IClientSecretValidator clientValidator,
            ITokenRequestValidator requestValidator,
            IEventService events,
            ILogger<TokenWithAuthFactorEndpoint> logger,
            ISystemClock systemClock,
            ITokenCreationService tokenCreationService,
            ITokenResponseGenerator responseGenerator,
            IMediator mediator,
            ICookieAuthSessionService cookieAuthSessionService)
        {
            this.clientValidator = clientValidator;
            this.requestValidator = requestValidator;
            this.events = events;
            this.logger = logger;
            this.systemClock = systemClock;
            this.tokenCreationService = tokenCreationService;
            this.responseGenerator = responseGenerator;
            this.mediator = mediator;
            this.cookieAuthSessionService = cookieAuthSessionService;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            this.logger.LogTrace(nameof(TokenWithAuthFactorEndpoint), "Processing token request.");

            // validate HTTP
            if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.HasFormContentType)
            {
                this.logger.LogWarning(nameof(TokenWithAuthFactorEndpoint),
                    "Invalid HTTP request for token with auth endpoint");
                return Error(OidcConstants.TokenErrors.InvalidRequest);
            }

            var clientResult = await this.clientValidator.ValidateAsync(context);
            if (clientResult.Client == null)
            {
                return Error(OidcConstants.TokenErrors.InvalidClient);
            }

            // validate request
            var form = (await context.Request.ReadFormAsync()).AsNameValueCollection();
            var requestResult = await this.requestValidator.ValidateRequestAsync(form, clientResult);

            if (requestResult.IsError)
            {
                var failEvent = new TokenIssuedFailureEvent(requestResult);
                if (!string.IsNullOrEmpty(failEvent.GrantType))
                {
                    await this.events.RaiseAsync(failEvent);
                }

                return Error(requestResult.Error, requestResult.ErrorDescription, requestResult.CustomResponse);
            }

            AuthUserInfo user = null;
            var email = GetUserName(requestResult);
            if (!string.IsNullOrWhiteSpace(email))
            {
                user = await this.mediator.Send(new GetAuthUserInfoQuery(email.ToLower()));
            }

            var id = GetConfirmToken(requestResult);
            if (user == null && int.TryParse(id, out int userId))
            {
                user = await this.mediator.Send(new GetAuthUserInfoQuery(userId));
            }

            var result = await GenerateNormalToken(requestResult);
            if (user != null)
            {
                await this.cookieAuthSessionService.SignInAsync(context, CreatePrincipal(user));
            }

            return new TokenResult(result, user.FirstName, user.LastName, user.Email, user.Phone, user.IdNumber, user.Roles, user.Organizations);
        }

        private static ClaimsPrincipal CreatePrincipal(AuthUserInfo user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtClaimTypes.Subject, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
                new Claim(JwtClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString())
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
                claims.Add(new Claim(JwtClaimTypes.Email, user.Email));
            }

            foreach (var role in user.Roles ?? Array.Empty<Lus.Contracts.Roles.AuthRoleDto>())
            {
                if (!string.IsNullOrWhiteSpace(role.Name))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.Name));
                    claims.Add(new Claim(Lus.Authorization.AuthConstants.ClaimTypes.UserRole, role.Name));
                }
            }

            var identity = new ClaimsIdentity(claims, CookieAuthSchemes.Api, ClaimTypes.Name, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }

        private static string GetUserName(TokenRequestValidationResult requestResult) =>
            requestResult.ValidatedRequest.UserName ??
            requestResult.ValidatedRequest.Subject?.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Email)?.Value
            ?? requestResult.ValidatedRequest.ClientClaims?.FirstOrDefault(c => c.Type == JwtClaimTypes.Email)?.Value;

        private static string GetConfirmToken(TokenRequestValidationResult requestResult) =>
            requestResult.ValidatedRequest.Subject?.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Subject)?.Value;

        private static TokenErrorResult Error(string error, string errorDescription = null, Dictionary<string, object> custom = null)
        {
            var response = new TokenErrorResponse
            {
                Error = error,
                ErrorDescription = errorDescription,
                Custom = custom
            };

            return new TokenErrorResult(response);
        }

        private async Task<TokenResponse> GenerateLimitedToken(Client client, AuthUserInfo user, string issuer,
            string scope)
        {
            var token = new Token
            {
                CreationTime = this.systemClock.UtcNow.UtcDateTime,
                Issuer = issuer,
                Lifetime = 60 * 60, // 60 minutes
                Claims = new List<Claim>(),
                ClientId = client.ClientId,
                AccessTokenType = AccessTokenType.Jwt,
                Audiences = { ApplicationConstants.ResourceApiNames.Platform }
            };

            token.Claims.Add(new Claim(JwtClaimTypes.ClientId, client.ClientId));
            token.Claims.Add(new Claim(JwtClaimTypes.Email, user.UserName));
            token.Claims.Add(new Claim(JwtClaimTypes.Name, user.UserName));
            token.Claims.Add(new Claim(JwtClaimTypes.Subject, user.Id.ToString()));
            token.Claims.Add(new Claim(JwtClaimTypes.Scope, scope));

            var stringToken = await this.tokenCreationService.CreateTokenAsync(token);

            var response = new TokenResponse
            {
                AccessToken = stringToken,
                AccessTokenLifetime = token.Lifetime,
                Scope = scope
            };

            return response;
        }

        private async Task<TokenResponse> GenerateNormalToken(TokenRequestValidationResult requestResult)
        {
            var response = await this.responseGenerator.ProcessAsync(requestResult);

            await this.events.RaiseAsync(new TokenIssuedSuccessEvent(response, requestResult));

            return response;
        }
    }
}
