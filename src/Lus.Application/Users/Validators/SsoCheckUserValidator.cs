using FluentValidation;
using Lus.Application.Common.Extensions;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Validators
{
    public class SsoCheckUserValidator : AbstractValidator<SsoCheckUserDto>
    {
        public SsoCheckUserValidator()
        {
            RuleFor(x => x.Email).EmailValidate();
        }
    }
}
