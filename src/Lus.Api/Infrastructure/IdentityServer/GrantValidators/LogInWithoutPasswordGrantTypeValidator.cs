using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using MediatR;
using Lus.Application;
using Lus.Application.Common.Ports;
using Lus.Application.Common.Services;
using Lus.Application.Users.Commands.LoginUserByToken;

namespace Lus.Infrastructure.IdentityServer.GrantValidators
{
    public class LogInWithoutPasswordGrantTypeValidator : IExtensionGrantValidator
    {
        private readonly IMediator mediator;
        private readonly IRecaptchaAdapter recaptchaAdapter;
        private readonly IUsersService usersService;

        public LogInWithoutPasswordGrantTypeValidator(IMediator mediator, IRecaptchaAdapter recaptchaAdapter, IUsersService usersService)
        {
            this.mediator = mediator;
            this.recaptchaAdapter = recaptchaAdapter;
            this.usersService = usersService;
        }

        public string GrantType => ApplicationConstants.ExternalGrandTypes.LoginWithoutPassword;

        public const string SmsCode = "sms_code";

        public Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var result = this.recaptchaAdapter.CheckRecaptcha().Result;
            if (!result)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 41 } });
            }

            var smsCode = context.Request.Raw.Get(SmsCode);
            if (smsCode == null)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 20 } });
                return Task.CompletedTask;
            }

            var lockedResult = this.usersService.IsAccountLockedAsync(smsCode).Result;
            if (!lockedResult.IsUserFound)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty,
                    new Dictionary<string, object> { { "exceptionId", 20 } });
                return Task.CompletedTask;
            }

            if (lockedResult.IsLocked)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 101 },
                    { "lockTimeLeft", lockedResult.LockTimeLeft }, { "isLocked", true} });
                return Task.CompletedTask;
            }

            var userDto = this.mediator.Send(new LoginUserByTokenCommand(smsCode)).Result;

            if (userDto is { IsConfirmed: false })
            {
                new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 11 } });
                return Task.CompletedTask;
            }

            context.Result = userDto != null ? new GrantValidationResult(userDto.Id.ToString(), OidcConstants.AuthenticationMethods.MultiFactorAuthentication) :
                new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 22 } });

            return Task.CompletedTask;
        }
    }
}
