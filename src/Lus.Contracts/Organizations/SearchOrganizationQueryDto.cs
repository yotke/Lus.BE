
namespace Lus.Contracts.Organizations
{
    public class SearchOrganizationQueryDto
    {
        public int? OrganizationId { get; set; }

        public string? OrganizationName { get; set; }

        public int? CityId { get; set; }

        public int? AreaId { get; set; }

        public bool? Active { get; set; }

        public int? AccountingId { get; set; }

    }
}
