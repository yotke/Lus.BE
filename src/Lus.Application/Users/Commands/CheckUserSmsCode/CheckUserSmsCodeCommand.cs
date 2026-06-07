using MediatR;

namespace Lus.Application.Users.Commands.CheckUserSmsCode
{
    public record CheckUserSmsCodeCommand(string SmsCode, string Email) : IRequest<Unit>;
}
