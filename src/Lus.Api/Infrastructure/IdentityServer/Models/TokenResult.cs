using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using Lus.Contracts.Organizations;
using Lus.Contracts.Roles;
using Lus.Infrastructure.Common;

namespace Lus.Infrastructure.IdentityServer.Models
{
    public class TokenResult : IEndpointResult
    {
        public TokenResponse Response { get; set; }

        public string FirstName { get; set; }

        public string Phone { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string IdNumber { get; set; }

        public ICollection<AuthRoleDto> Roles { get; set; }

        public ICollection<AuthOrganizationDto> Organizations { get; set; }

        public TokenResult()
        {
        }

        public TokenResult(TokenResponse response) =>
            Response = response ?? throw new ArgumentNullException(nameof(response));

        public TokenResult(TokenResponse response, string firstName, string lastName, string email, string phone, string idNumber, ICollection<AuthRoleDto> roles, ICollection<AuthOrganizationDto> organizations)
            : this(response)
        {
            FirstName = firstName;
            LastName = lastName;
            Phone = phone;
            IdNumber = idNumber;
            Email = email;
            Roles = roles;
            Organizations = organizations;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            context.Response.SetNoCache();

            var dto = new TokenResultDto
            {
                IdToken = Response.IdentityToken,
                AccessToken = Response.AccessToken,
                RefreshToken = Response.RefreshToken,
                ExpiresIn = Response.AccessTokenLifetime,
                TokenType = OidcConstants.TokenResponse.BearerTokenType,
                Scope = Response.Scope,
                Email = Email,
                FirstName = FirstName,
                LastName = LastName,
                Phone = Phone,
                IdNumber = IdNumber,
                Roles = Roles,
                Organizations = Organizations,
                Custom = Response.Custom
            };

            await context.WriteJsonAsync(dto);
        }
    }
}
