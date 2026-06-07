using FluentValidation;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Extensions;
using Lus.Application.Common.Options;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Validators
{
    public class UserResetPasswordFromSmsValidator : AbstractValidator<UserResetPasswordFromSmsDto>
    {
        private readonly PasswordConfigOptions passwordConfigOptions;

        public UserResetPasswordFromSmsValidator(IOptions<PasswordConfigOptions> options)
        {
            this.passwordConfigOptions = options.Value;

            RuleFor(x => x.Email).EmailValidate();
            RuleFor(x => x.Password).PasswordValidate(this.passwordConfigOptions);
            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
            RuleFor(x => x.SmsCode).NotEmpty().Length(6, 10);
        }
    }
}
