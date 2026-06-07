using FluentValidation;
using Lus.Application.Common.Extensions;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Validators
{
    public class UserResetPasswordValidator : AbstractValidator<UserResetPasswordDto>
    {
        public UserResetPasswordValidator()
        {
            RuleFor(x => x.Email).EmailValidate();
            RuleFor(x => x.ResetPasswordType).NotEmpty();
        }
    }
}
