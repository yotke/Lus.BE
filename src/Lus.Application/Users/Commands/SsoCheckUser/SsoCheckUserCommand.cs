using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.SsoCheckUser
{
    public record SsoCheckUserCommand(string Email) : IRequest<UserCheckedDto>;
}
