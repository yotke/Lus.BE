using MediatR;

namespace Lus.Application.Users.Commands.CheckEmailToken
{
    public record CheckEmailTokenCommand(string EmailToken) : IRequest<Unit>;
}