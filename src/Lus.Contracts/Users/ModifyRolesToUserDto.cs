using Lus.Contracts.Roles;

namespace Lus.Contracts.Users
{
    public class ModifyRolesToUserDto
    {
        public ICollection<RoleDto>? Roles { get; set; }
        public int UserId { get; set; }
    }
}
