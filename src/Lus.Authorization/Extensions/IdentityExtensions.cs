using IdentityModel;
using System.Globalization;
using System.Security.Claims;
using System.Security.Principal;

namespace Lus.Authorization.Extensions
{
    public static class IdentityExtensions
    {
        public static T? GetUserId<T>(this IIdentity identity) where T : IConvertible
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (identity is ClaimsIdentity identity1)
            {
                var firstValue = identity1.FindFirst(ClaimTypes.NameIdentifier);
                if (firstValue != null)
                {
                    return (T)Convert.ChangeType(firstValue.Value, typeof(T), CultureInfo.InvariantCulture);
                }
            }
            return default(T);
        }

        public static bool TryGetUserId<T>(this IIdentity identity, out T? value)
            where T : IConvertible
        {
            if (identity is ClaimsIdentity claimIdentity)
            {
                var firstValue = claimIdentity.FindFirst(ClaimTypes.NameIdentifier) ?? claimIdentity.FindFirst(JwtClaimTypes.Subject);
                if (firstValue != null)
                {
                    value = (T)Convert.ChangeType(firstValue.Value, typeof(T), CultureInfo.InvariantCulture);
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static T? GetUserPermission<T>(this IEnumerable<Claim> claims)
            where T : IConvertible => GetClaimValue<T>(claims, CustomClaimTypes.Permission);

        public static T? GetUserName<T>(this ClaimsPrincipal principal) where T : IConvertible
        {
            return GetClaimValue<T>(principal, JwtClaimTypes.Name) ?? GetClaimValue<T>(principal, ClaimTypes.Name);
        }

        private static T? GetClaimValue<T>(ClaimsPrincipal principal, string claimType) where T : IConvertible
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            var firstValue = principal.FindFirst(claimType);
            if (firstValue != null)
            {
                return (T)Convert.ChangeType(firstValue.Value, typeof(T), CultureInfo.InvariantCulture);
            }

            return default(T);
        }

        public static T? GetClaimValue<T>(this IEnumerable<Claim> claims, string claimType) where T : IConvertible
        {
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            var firstValue = claims.FirstOrDefault(c => c.Type.Equals(claimType));
            if (firstValue != null)
            {
                return (T)Convert.ChangeType(firstValue.Value, typeof(T), CultureInfo.InvariantCulture);
            }

            return default(T);
        }

        private static IEnumerable<T> GetClaimValuesCollection<T>(ClaimsPrincipal principal, string claimType)
            where T : IConvertible
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            var claimValues = principal.FindAll(claimType);

            return claimValues.Select(value => (T)Convert.ChangeType(value.Value, typeof(T), CultureInfo.InvariantCulture)).ToList();
        }

        private static IEnumerable<T> GetClaimValuesCollection<T>(this IEnumerable<Claim> claims, string claimType)
            where T : IConvertible
        {
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            var claimValues = claims.Where(c => c.Type.Equals(claimType)).ToList();

            return claimValues.Select(value => (T)Convert.ChangeType(value.Value, typeof(T), CultureInfo.InvariantCulture)).ToList();
        }
    }
}
