using FluentValidation;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Validators
{
    public class CheckEmailTokenValidator : AbstractValidator<CheckEmailTokenDto>
    {
        public CheckEmailTokenValidator()
        {
            RuleFor(x => x.EmailToken).NotEmpty();
        }
    }
}
