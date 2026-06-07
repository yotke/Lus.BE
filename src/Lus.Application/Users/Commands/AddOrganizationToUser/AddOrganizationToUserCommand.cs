using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.AddOrganizationToUser
{
    public record AddOrganizationToUserCommand(ICollection<int> OrganizationsId, int? UserId) : IRequest<UserFullInfoDto>;
}
