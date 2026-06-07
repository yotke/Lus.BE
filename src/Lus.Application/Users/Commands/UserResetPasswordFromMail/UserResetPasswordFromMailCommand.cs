using MediatR;

namespace Lus.Application.Users.Commands.UserResetPasswordFromMail
{
    public record UserResetPasswordFromMailCommand(string PasswordVerificationToken, string Password) : IRequest<Unit>;
}
