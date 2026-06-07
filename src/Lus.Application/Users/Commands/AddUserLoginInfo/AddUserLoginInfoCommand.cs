using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.AddUserLoginInfo
{
    public record AddUserLoginInfoCommand(UserDto User) : IRequest<Unit>;
}
