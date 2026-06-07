using MediatR;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations.Queries.GetOrganizationsToUser
{
    public record GetOrganizationsToUserQuery : IRequest<ICollection<OrganizationToManageUserDto>>
    {
        public string? OrganizationName { get; init; }

        public string? OrganizationId { get; init; }

        public int? CityNumeriId { get; init; }

        public string? AccountingNumber { get; init; }

        public int? AreaNumeriName { get; init; }

        public bool? Active { get; init; }
    };
}
