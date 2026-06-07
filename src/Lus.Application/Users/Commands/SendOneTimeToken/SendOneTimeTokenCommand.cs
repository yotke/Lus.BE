using MediatR;

namespace Lus.Application.Users.Commands.SendOneTimeToken
{
    public record SendOneTimeTokenCommand(string Email, string Phone) : IRequest<Unit>;
}
