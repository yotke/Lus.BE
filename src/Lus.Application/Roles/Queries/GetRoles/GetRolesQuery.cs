using MediatR;
using Lus.Contracts.Roles;

namespace Lus.Application.Roles.Queries.GetRoles
{
    public record GetRolesQuery(bool IgnoreOrganization = false) : IRequest<ICollection<RoleDto>>;
}
