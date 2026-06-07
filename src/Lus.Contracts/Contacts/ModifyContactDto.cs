namespace Lus.Contracts.Contacts
{
    public class ModifyContactDto
    {
        public int Id { get; set; }

        public string IdNumber { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public int? OrganizationId { get; set; }
    }
}
