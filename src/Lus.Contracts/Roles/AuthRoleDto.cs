namespace Lus.Contracts.Roles
{
    public class AuthRoleDto
    {
        public string Name { get; set; }

        public int Immunity { get; set; }

        public int? OrganizationId { get; set; }
    }
}
