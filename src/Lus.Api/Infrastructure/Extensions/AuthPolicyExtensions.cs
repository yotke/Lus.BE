using Microsoft.AspNetCore.Authorization;
using Lus.Application;

namespace Lus.Infrastructure.Extensions
{
    public static class AuthPolicyExtensions
    {
        public static AuthorizationPolicyBuilder RequireAllowance(
            this AuthorizationPolicyBuilder builder,
            params string[] allowances) =>
            builder.RequireClaim(ApplicationConstants.ClaimsTypes.Allowance, allowances);
    }
}
