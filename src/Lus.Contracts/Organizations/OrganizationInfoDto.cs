
using Lus.Contracts.Cities;

namespace Lus.Contracts.Organizations
{
    public class OrganizationInfoDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Langitude { get; set; }

        public string Lotitude { get; set; }

        public bool Active { get; set; }

        public int AccountingNumber { get; set; }

        public int? CityId { get; set; }
        
        public CityDto city { get; set; }

    }
}
