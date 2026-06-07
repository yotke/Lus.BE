using Microsoft.AspNetCore.Authorization;

namespace Lus.Infrastructure.IdentityServer.Permissions
{
    public class PermissionAttribute : AuthorizeAttribute
    {
        const string POLICY_PREFIX = "Permission";

        public PermissionAttribute(string requiredPermission) =>
            RequiredPermission = requiredPermission;

        public string? RequiredPermission
        {
            get => Policy?.Substring(POLICY_PREFIX.Length);
            set => Policy = $"{POLICY_PREFIX}{value}";
        }
    }
}
