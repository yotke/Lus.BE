using Lus.Contracts.Organizations;
using Lus.Contracts.Roles;

namespace Lus.Contracts.Users
{
    public class UserFullInfoDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string IdNumber { get; set; }

        public ICollection<RoleDto> Roles { get; set; }

        public ICollection<OrganizationDto> Organizations { get; set; }
    }
}
