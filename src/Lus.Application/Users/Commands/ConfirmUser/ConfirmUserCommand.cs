using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.ConfirmUser
{
    public record ConfirmUserCommand(string ConfirmToken) : IRequest<UserDto>;
}
