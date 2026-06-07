namespace Lus.Authorization.Extensions
{
    internal static class UserAccessorExtensions
    {
        public static int? GetRoleId(this IUserAccessor userAccessor)
        {
            var roleIdClaim = userAccessor.GetClaimValue("role");

            return !int.TryParse(roleIdClaim, out var roleId) ? default(int?) : roleId;
        }

        private static string GetClaimValue(this IUserAccessor userAccessor, string claimName)
        {
            var claimValue = userAccessor?.ProjectUser.Claims
                .FirstOrDefault(c => string.Equals(c.Item1, claimName, StringComparison.InvariantCultureIgnoreCase));

            return claimValue?.Item2;
        }

        public static int GetUserIdOrThrow(this IUserAccessor userAccessor, Exception exceptionToThrow)
        {
            var userId = userAccessor.ProjectUser?.Id ?? throw exceptionToThrow;
            return userId;
        }
    }
}
