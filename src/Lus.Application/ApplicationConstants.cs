namespace Lus.Application
{
    public static class ApplicationConstants
    {
        public static class DefaultTopicUnicode
        {
            public static string ProfessionalRating = "UNITCODE";
            public static string ProfessionArea = "SUBSIDIARY";
            public static string ProfessionsList = "ROLE";
        }

        public static class ActionModelNames
        {
            public static string CreateUser = "createUserDto";
            public static string CheckPassword = "checkPasswordDto";
            public static string SsoCheckUser = "ssoCheckUserDto";
            public static string UserResetPassword = "userResetPasswordDto";
            public static string UserPasswordFromSms = "passwordFromSmsDto";
            public static string CheckUserSmsCode = "checkUserSmsCodeDto";
            public static string UserPasswordFromMail = "passwordFromMailDto";
            public static string SendOneTimeToken = "sendOneTimeTokenDto";
        }

        public static class Scopes
        {
            public const string SecondAuthenticationFactor = "authentication:secondfactor";
            public const string PublicApiScope = "publicapi";
            public const string InternalApi = "internalapi";
            public const string Private = "private.api";
            public const string OfflineAccess = "offline_access";
        }

        public static class ExternalGrandTypes
        {
            public const string ConfirmTokenGrantType = "confirm_token";
            public const string LoginWithoutPassword = "login_by_token";
        }

        public static class ApplicationClaimsType
        {
            public const string ClientId = "client_id";
            public const string ClientType = "clienttype";
            public const string Allowance = "allowance";
        }

        public static class ClaimsTypes
        {
            public const string Permission = "permission";
            public const string Allowance = "allowance";
            public const string Source = "source";
            public const string Scope = "scope";
            public const string AuthenticationMethodsReferences = "amr";
            public const string Sub = "sub";
            public const string UserId = "user_id";
            public const string AuthenticationMethod = "http://schemas.microsoft.com/claims/authnmethodsreferences";
            public const string UserRole = "userrole";
        }

        public static class Allowances
        {
        }

        public static class ResourceApiNames
        {
            public const string Platform = "platform.api";
            public const string Public = "identity.public.api";
        }

        public static class AllowedGrandTypes
        {
            public const string ClientCredentials = "client_credentials";
            public const string Password = "password";
        }

        public static class Allowance
        {
            public const string DeleteUser = "can_delete_user";
        }

        public static class AuthPolicies
        {
            public const string ReadWriteData = "ReadWriteData";
            public const string SensetiveOperation = "SensetiveOperation";
            public const string AdminRoles = "AdminRoles";
            public const string AdminRolesAndCommitteesManager = "AdminRolesAndCommitteesManager";
            public const string ChangeUserPassword = "ChangeUserPassword";
            public const string BasicAuthentication = "BasicAuthentication";
            public const string PrivateApi = "PrivateApi";
            public const string ImpersonateUser = "ImpersonateUser";
        }

        public static class OptionSections
        {
            public const string MassTransit = "MassTransit";
            public const string General = "General";
            public const string UserLoginAuditing = "UserLoginAuditing";
            public const string RecaptchaConfigOptions = "RecaptchaSettings";
            public const string PasswordConfigOptions = "PasswordConfigOptions";
            public const string ApplicationOptions = "ApplicationOptions";
            public const string SmsNotificationOptions = "SmsNotificationOptions";
            public const string MailNotificationOptions = "MailNotificationOptions";
            public const string GoogleAuthOptions = "Authentication:Google";
        }

        public static class NotificationConstants
        {
            public const string MailSender = "Lus@iula.org.il";
        }

        public static class ClaimsValues
        {
            public const string NoRole = "-1";
            public const string SiteAdmin = "SITEADMIN";
            public const string Admin = "ADMIN";
            public const string Developer = "DEVELOPER";
            public const string CommitteesManager = "COMMITTEES_MANAGER";
        }

        public static class CachedProviderKeys
        {
            public const string UserOrganizationCacheKey = "user_organization_id_";
            public const string TendersSearchCacheKey = "tenders_search_term_";
        }
    }
}
