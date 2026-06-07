using System.Security.Claims;
using Lus.Contracts.Roles;
using Lus.Contracts.Users;

namespace Lus.Authorization.Authentication
{
    /// <summary>
    /// Builds the <see cref="ClaimsPrincipal"/> that is persisted in the auth cookie after a
    /// successful login. It attaches every claim the existing authorization policies rely on:
    /// the user id (<see cref="ClaimTypes.NameIdentifier"/> and <c>sub</c>), name/email, roles
    /// (both <see cref="ClaimTypes.Role"/> and the organization-prefixed <c>userrole</c> claim
    /// used by the permission handler), permission claims and the API <c>scope</c> claims that
    /// the scope-based named policies require.
    /// </summary>
    public static class CookiePrincipalFactory
    {
        private const string ScopeClaimType = "scope";
        private const string InternalApiScope = "internalapi";
        private const string PublicApiScope = "publicapi";

        public static ClaimsPrincipal Create(AuthUserInfo user, string authenticationScheme = null)
        {
            ArgumentNullException.ThrowIfNull(user);

            var scheme = string.IsNullOrWhiteSpace(authenticationScheme)
                ? CookieAuthSchemes.Api
                : authenticationScheme;

            var displayName = user.UserName ?? user.Email ?? user.Id.ToString();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(AuthConstants.ClaimTypes.Sub, user.Id.ToString()),
                new(ClaimTypes.Name, displayName),
                new("name", displayName),
                new(ScopeClaimType, InternalApiScope),
                new(ScopeClaimType, PublicApiScope)
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
                claims.Add(new Claim("email", user.Email));
            }

            foreach (var role in user.Roles ?? new List<AuthRoleDto>())
            {
                if (string.IsNullOrWhiteSpace(role.Name))
                {
                    continue;
                }

                claims.Add(new Claim(ClaimTypes.Role, role.Name));
                // The permission handler matches "{organizationId}{ROLE_NAME}".
                claims.Add(new Claim(AuthConstants.ClaimTypes.UserRole, $"{role.OrganizationId}{role.Name}"));
            }

            foreach (var claim in user.Claims ?? new List<KeyValuePair<string, string>>())
            {
                if (string.Equals(claim.Key, AuthConstants.ClaimTypes.Permission, StringComparison.OrdinalIgnoreCase))
                {
                    claims.Add(new Claim(AuthConstants.ClaimTypes.Permission, claim.Value));
                }
            }

            var identity = new ClaimsIdentity(claims, scheme, ClaimTypes.Name, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }
    }
}

