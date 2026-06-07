using FluentValidation;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Extensions;
using Lus.Application.Common.Options;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Validators
{
    public class UserResetPasswordFromMailValidator : AbstractValidator<UserResetPasswordFromMailDto>
    {
        private readonly PasswordConfigOptions passwordConfigOptions;

        public UserResetPasswordFromMailValidator(IOptions<PasswordConfigOptions> options)
        {
            this.passwordConfigOptions = options.Value;

            RuleFor(x => x.Password).PasswordValidate(this.passwordConfigOptions);
            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
            RuleFor(x => x.PasswordVerificationToken).NotEmpty();
        }
    }
}
