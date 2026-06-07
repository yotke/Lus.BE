using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using MediatR;
using Lus.Application;
using Lus.Application.Common.Ports;
using Lus.Application.Users.Commands.ConfirmUser;

namespace Lus.Infrastructure.IdentityServer.GrantValidators
{
    public class ConfirmTokenGrantTypeValidator : IExtensionGrantValidator
    {
        private readonly IMediator mediator;
        private readonly IRecaptchaAdapter recaptchaAdapter;

        public ConfirmTokenGrantTypeValidator(IMediator mediator, IRecaptchaAdapter recaptchaAdapter)
        {
            this.mediator = mediator;
            this.recaptchaAdapter = recaptchaAdapter;
        }

        public string GrantType => ApplicationConstants.ExternalGrandTypes.ConfirmTokenGrantType;

        public const string ConfirmToken = "confirm_token";

        public Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var result = this.recaptchaAdapter.CheckRecaptcha().Result;
            if (!result)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 41 } });
            }

            var confirmToken = context.Request.Raw.Get(ConfirmToken);
            if (confirmToken == null)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 20 } });
                return Task.CompletedTask;
            }

            var userDto = this.mediator.Send(new ConfirmUserCommand(confirmToken)).Result;
            context.Result = userDto != null ? new GrantValidationResult(userDto.Id.ToString(), OidcConstants.AuthenticationMethods.MultiFactorAuthentication) :
                new GrantValidationResult(TokenRequestErrors.InvalidGrant, string.Empty, new Dictionary<string, object> { { "exceptionId", 20 } });

            return Task.CompletedTask;
        }
    }
}
