using MediatR;
using Lus.Contracts.Roles;

namespace Lus.Application.Roles.Commands.ModifyRoles
{
    public record ModifyRolesCommand(ICollection<ModifyRoleDto> Roles) : IRequest<ICollection<RoleDto>>;
}
