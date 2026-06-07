using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.AddRoleToUser
{
    public record AddRoleToUserCommand(ICollection<int> RolesId, int? UserId,int OrganizationId) : IRequest<UserFullInfoDto>;
}
