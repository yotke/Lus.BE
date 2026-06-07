using Microsoft.AspNetCore.Authorization;

namespace Lus.Infrastructure.IdentityServer.Permissions
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(string permission) =>
            Permission = permission;

        public string Permission { get; }
    }
}
