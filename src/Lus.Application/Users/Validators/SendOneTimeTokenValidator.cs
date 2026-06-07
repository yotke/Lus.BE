using FluentValidation;
using Lus.Application.Common.Extensions;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Validators
{
    public class SendOneTimeTokenValidator : AbstractValidator<SendOneTimeTokenDto>
    {
        public SendOneTimeTokenValidator()
        {
            RuleFor(x => x.Phone).NotEmpty();
            RuleFor(x => x.Email).EmailValidate();
        }
    }
}
