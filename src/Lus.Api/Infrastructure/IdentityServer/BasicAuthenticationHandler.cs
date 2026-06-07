using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Lus.Application;
using Lus.Application.Common.Services;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Lus.Infrastructure.IdentityServer
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IUsersService usersService;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock, IUsersService usersService) :
            base(options, logger, encoder, clock)
        {
            this.usersService = usersService;
        }
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // skip authentication if endpoint has [AllowAnonymous] attribute
            var endpoint = Context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                return AuthenticateResult.NoResult();
            }

            var authQuery = Request.Query["access_token"].FirstOrDefault();

            if (!Request.Headers.ContainsKey("Authorization") && string.IsNullOrWhiteSpace(authQuery))
            {
                return AuthenticateResult.Fail("Missing Authorization Header");
            }

            try
            {
                var authHeader = Request.Headers["Authorization"];
                if (authHeader.ToString().Contains("Bearer"))
                {
                    var token = authHeader.ToString().Substring("Bearer ".Length).Trim();

                    var handler = new JwtSecurityTokenHandler();
                    var jwtSecurityToken = handler.ReadJwtToken(token);
                    var claimsBearer = new[] {
                        new Claim(ClaimTypes.NameIdentifier, jwtSecurityToken.Subject),
                        new Claim( ApplicationConstants.ClaimsTypes.Scope, ApplicationConstants.Scopes.InternalApi),
                    };
                    var identityBearer = new ClaimsIdentity(claimsBearer, Scheme.Name);
                    var principalBearer = new ClaimsPrincipal(identityBearer);
                    var ticketBearer = new AuthenticationTicket(principalBearer, Scheme.Name);

                    return AuthenticateResult.Success(ticketBearer);
                }

                if (!string.IsNullOrWhiteSpace(authQuery))
                {
                    var handler = new JwtSecurityTokenHandler();

                    var jwtSecurityToken = handler.ReadJwtToken(authQuery);
                    var claimsBearer = new[] {
                        new Claim(ClaimTypes.NameIdentifier, jwtSecurityToken.Subject),
                        new Claim( ApplicationConstants.ClaimsTypes.Scope, ApplicationConstants.Scopes.InternalApi),
                    };
                    var identityBearer = new ClaimsIdentity(claimsBearer, Scheme.Name);
                    var principalBearer = new ClaimsPrincipal(identityBearer);
                    var ticketBearer = new AuthenticationTicket(principalBearer, Scheme.Name);

                    return AuthenticateResult.Success(ticketBearer);
                }

            }
            catch (Exception e)
            {
                return AuthenticateResult.Fail("Invalid token");
            }

            string username = string.Empty;
            string password = string.Empty;
            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
                username = credentials[0];
                password = credentials[1];
            }
            catch
            {
                return AuthenticateResult.Fail("Invalid Authorization Header");
            }

            var loginResult = await this.usersService.LoginAsync(username, password);
            if (!loginResult.IsValidCredentials)
                return AuthenticateResult.Fail("Invalid Username or Password");

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, loginResult.UserId.ToString()),
                new Claim( ApplicationConstants.ClaimsTypes.Scope, ApplicationConstants.Scopes.InternalApi),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
