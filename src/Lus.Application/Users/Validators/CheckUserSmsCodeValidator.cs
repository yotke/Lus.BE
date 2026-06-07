using FluentValidation;
using Lus.Application.Common.Extensions;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Validators
{
    public class CheckUserSmsCodeValidator : AbstractValidator<CheckUserSmsCodeDto>
    {
        public CheckUserSmsCodeValidator()
        {
            RuleFor(x => x.Email).EmailValidate();
            RuleFor(x => x.SmsCode).NotEmpty().Length(6, 10);
        }
    }
}
