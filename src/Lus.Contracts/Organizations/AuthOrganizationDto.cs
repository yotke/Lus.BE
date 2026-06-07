namespace Lus.Contracts.Organizations
{
    public class AuthOrganizationDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? CityId { get; set; }

        public int AccountingNumber { get; set; }
    }
}
