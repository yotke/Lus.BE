using MediatR;

namespace Lus.Application.Users.Commands.CheckPassword
{
    public record CheckPasswordCommand(string Password) : IRequest<Unit>;
}
