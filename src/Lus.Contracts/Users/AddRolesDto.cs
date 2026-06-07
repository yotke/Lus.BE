using Lus.Contracts.Roles;

namespace Lus.Contracts.Users
{
    public class AddRolesToUserDto
    {
        public ICollection<int>? RolesId { get; set; }

        public int OrganizationId { get; set; }

        public int? UserId { get; set; }
    }
}
