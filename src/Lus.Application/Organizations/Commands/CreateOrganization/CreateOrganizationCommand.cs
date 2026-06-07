using MediatR;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations.Commands.CreateOrganization
{
    public record CreateOrganizationCommand : IRequest<OrganizationDto>
    {
        public string Name { get; set; }

        public bool Active { get; set; }

        public int Langitude { get; set; }

        public string Lotitude { get; set; }

        public int? GeoRegionId { get; set; }

        public int? CityId { get; set; }

        public int AccountingNumber { get; set; }

        public int? MunicipalityRankId { get; set; }
    }
}
