using Lus.Contracts.Cities;
using Lus.Contracts.Contacts;
namespace Lus.Contracts.Organizations
{
    public class ModifyOrganizationDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Langitude { get; set; }

        public string Lotitude { get; set; }
        
        public bool? Active { get; set; }

        public int AccountingNumber { get; set; }

        public int? CityId { get; set; }

        public CityDto? City { get; set; }

        public ICollection<ModifyContactDto>? Contacts { get; set; }

    }
}
