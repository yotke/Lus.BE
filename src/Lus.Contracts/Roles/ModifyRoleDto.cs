namespace Lus.Contracts.Roles
{
    public class ModifyRoleDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Immunity { get; set; }

        public string HebrewName { get; set; }

        public bool ShowToAdmin { get; set; }

        public int? OrganizationId { get; set; }
    }
}
