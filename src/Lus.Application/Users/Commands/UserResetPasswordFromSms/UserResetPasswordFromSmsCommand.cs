using MediatR;

namespace Lus.Application.Users.Commands.UserResetPasswordFromSms
{
    public record UserResetPasswordFromSmsCommand(string Email, string SmsCode, string Password) : IRequest<Unit>;
}
