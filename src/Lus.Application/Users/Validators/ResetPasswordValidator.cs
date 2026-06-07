using FluentValidation;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Extensions;
using Lus.Application.Common.Options;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Validators
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
    {
        private readonly PasswordConfigOptions passwordConfigOptions;

        public ResetPasswordValidator(IOptions<PasswordConfigOptions> options)
        {
            this.passwordConfigOptions = options.Value;

            RuleFor(x => x.Password).PasswordValidate(this.passwordConfigOptions);
            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
        }
    }
}
