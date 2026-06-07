using FluentValidation;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Extensions;
using Lus.Application.Common.Options;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Validators
{
    public class CheckPasswordValidator : AbstractValidator<CheckPasswordDto>
    {
        private readonly PasswordConfigOptions passwordConfigOptions;

        public CheckPasswordValidator(IOptions<PasswordConfigOptions> options)
        {
            this.passwordConfigOptions = options.Value;

            RuleFor(x => x.Password).PasswordValidate(this.passwordConfigOptions);
        }
    }
}
