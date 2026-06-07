using MediatR;
using Lus.Application.Roles.Entities;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.ModifyRoleToUser
{
    public record ModifyRoleToUserCommand(ICollection<Role> roles, int UserId) : IRequest<UserFullInfoDto>;
}
