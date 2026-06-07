using FluentValidation;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Extensions;
using Lus.Application.Common.Options;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Validators
{
    public class CreateUserValidator : AbstractValidator<CreateUserDto>
    {
        private readonly PasswordConfigOptions passwordConfigOptions;

        public CreateUserValidator(IOptions<PasswordConfigOptions> options)
        {
            this.passwordConfigOptions = options.Value;

            RuleFor(x => x.Email).EmailValidate();
            RuleFor(x => x.Password).PasswordValidate(this.passwordConfigOptions);
            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
            RuleFor(x => x.ConfirmEmail).Equal(x => x.Email);
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.IdNumber).NotEmpty();
            RuleFor(x => x.Phone).NotEmpty();
        }
    }
}
