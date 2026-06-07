using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUsersInfoByOrganization
{
    public record GetUsersInfoByOrganizationQuery : IRequest<ICollection<UserInfoDto>>
    {
        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public string? Email { get; init; }

        public string? Phone { get; init; }

        public string? IdNumber { get; init; }
    }
}
