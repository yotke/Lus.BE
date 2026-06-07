using System.Security.Claims;
using EasyCaching.Core;
using Microsoft.AspNetCore.Authorization;
using Lus.Application;
using Lus.Authorization;

namespace Lus.Infrastructure.IdentityServer.Permissions
{
    public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IUserAccessor userAccessor;
        private readonly IEasyCachingProvider provider;

        public PermissionRequirementHandler(IUserAccessor userAccessor, IEasyCachingProvider provider)
        {
            this.userAccessor = userAccessor;
            this.provider = provider;
        }

        private readonly Dictionary<string, List<string>> permissionClaimValueChecker = new Dictionary<string, List<string>>
        {
            { ApplicationConstants.AuthPolicies.AdminRoles, new List<string>{ ApplicationConstants.ClaimsValues.SiteAdmin, ApplicationConstants.ClaimsValues.Admin } },
            { ApplicationConstants.AuthPolicies.AdminRolesAndCommitteesManager, new List<string>{ ApplicationConstants.ClaimsValues.SiteAdmin, ApplicationConstants.ClaimsValues.Admin, ApplicationConstants.ClaimsValues.CommitteesManager } }
        };

        private readonly Dictionary<string, string> permissionClaimTypeChecker = new Dictionary<string, string>
        {
            { ApplicationConstants.AuthPolicies.AdminRoles, "userrole" },
            { ApplicationConstants.AuthPolicies.AdminRolesAndCommitteesManager, "userrole" }
        };

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var organizationId = await this.provider.GetAsync<int>(
                $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{this.userAccessor.ProjectUser.Id}");

            var claimsValues = permissionClaimValueChecker.GetValueOrDefault(requirement.Permission);
            var claimType = permissionClaimTypeChecker.GetValueOrDefault(requirement.Permission);

            if (claimType != null)
            {
                var claims = ((ClaimsIdentity)context.User.Identity)?.FindAll(claimType).Select(c => c.Value).ToList();
                if (claims == null || !claims.Any())
                {
                    return;
                }

                if (claimsValues != null && claimsValues.Any(x => claims.Contains($"{organizationId}{x}")))
                {
                    context.Succeed(requirement);
                }

                if (claims.Contains(ApplicationConstants.ClaimsValues.Developer) || claims.Contains(ApplicationConstants.ClaimsValues.SiteAdmin))
                {
                    context.Succeed(requirement);
                }
            }

            return;
        }
    }
}
