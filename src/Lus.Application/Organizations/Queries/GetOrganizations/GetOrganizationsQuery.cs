using MediatR;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations.Queries.GetOrganizations
{
    public record GetOrganizationsQuery : IRequest<ICollection<OrganizationInfoDto>>
    {
        public string? OrganizationName { get; init; }

        public string? OrganizationId { get; init; }

        public int? CityNumeriId { get; init; }

        public string? AccountingNumber { get; init; }

        public int? AreaNumeriName { get; init; }

        public bool? Active { get; init; }
    };
}
