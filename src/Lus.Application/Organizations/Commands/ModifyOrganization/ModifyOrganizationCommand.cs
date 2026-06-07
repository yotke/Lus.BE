using MediatR;
using Lus.Contracts.Contacts;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations.Commands.ModifyOrganization
{
    public record ModifyOrganizationCommand : IRequest<OrganizationDto>
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool Active { get; set; }

        public int Langitude { get; set; }

        public string Lotitude { get; set; }

        public int? GeoRegionId { get; set; }

        public int? CityId { get; set; }

        public int AccountingNumber { get; set; }

        public int? MunicipalityRankId { get; set; }

        public ICollection<ModifyContactDto> Contacts { get; set; }
    }
}
