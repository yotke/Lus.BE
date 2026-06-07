using Lus.Application.Common;
using Lus.Application.Roles.Entities;

namespace Lus.Application.Users.Entities
{
    public class UserRole : EntityBase<int>
    {
        public int? UserId { get; set; }

        public User User { get; set; }

        public int RoleId { get; set; }

        public Role Role { get; set; }
    }
}
