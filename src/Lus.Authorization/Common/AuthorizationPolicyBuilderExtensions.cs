using Microsoft.AspNetCore.Authorization;


namespace Lus.Authorization.Common
{
    public static class AuthorizationPolicyBuilderExtensions
    {
        public static AuthorizationPolicyBuilder RequirePermission(this AuthorizationPolicyBuilder builder, params string[] permission)
        {
            return builder.RequireClaim(CustomClaimTypes.Permission, permission);
        }

        public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, params string[] scope)
        {
            return builder.RequireClaim("scope", scope);
        }
    }
}
