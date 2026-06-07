namespace Lus.Authorization
{
    public static class AuthConstants
    {
        public static class Schemes
        {
            public const string ApiCookies = "api.cookies";
            public const string IdentityServerCookies = "idsrv.cookies";
            public const string Smart = "smart";
        }

        public static class CacheKeys
        {
            public const string UserOrganizationPrefix = "user_organization_id_";
        }

        public static class ClaimTypes
        {
            public const string Permission = "permission";
            public const string UserRole = "userrole";
            public const string Sub = "sub";
        }
    }
}
