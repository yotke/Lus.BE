using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Lus.Application.Common.Ports;
using Lus.Application.Common.Services;
using Lus.Contracts.Users.Types;

namespace Lus.Infrastructure.IdentityServer
{
    public class CustomResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly IUsersService usersService;
        private readonly IRecaptchaAdapter recaptchaAdapter;

        public CustomResourceOwnerPasswordValidator(IUsersService usersService, IRecaptchaAdapter recaptchaAdapter) => (this.usersService, this.recaptchaAdapter) = (usersService, recaptchaAdapter);

        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            var result = this.recaptchaAdapter.CheckRecaptcha().Result;
            if (!result)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Recaptcha is not valid", new Dictionary<string, object> { { "exceptionId", 41 } });
                return Task.CompletedTask;
            }

            var loginResult = this.usersService.LoginAsync(context.UserName.ToLower(), context.Password).Result;

            if (loginResult.LoginFailReason == LoginFailReasonType.UserNotFound)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 10 } });
                return Task.CompletedTask;
            }

            if (loginResult.LoginFailReason == LoginFailReasonType.WrongPassword)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 12 } });
                return Task.CompletedTask;
            }

            if (loginResult.LoginFailReason == LoginFailReasonType.BlockedUser)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 101 },
                    { "lockTimeLeft", loginResult.LockTimeLeft }, { "isLocked", true} });
                return Task.CompletedTask;
            }

            if (loginResult.LoginFailReason == LoginFailReasonType.UserNotConfirm)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 11 } });
                return Task.CompletedTask;
            }

            if (loginResult.LoginFailReason == LoginFailReasonType.PasswordExpired)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 13 }, { "exception", loginResult.ErrorMessage } });
                return Task.CompletedTask;
            }

            context.Result = new GrantValidationResult(loginResult.UserId.ToString(),
                OidcConstants.AuthenticationMethods.Password);
            return Task.CompletedTask;
        }

    }
}
