using Lus.Application.Common;

namespace Lus.Application.Users.Entities
{
    public class UserPasswordHistory : EntityBase<int>
    {
        public string PasswordHash { get; set; }

        public int? UserId { get; set; }

        public User User { get; set; }
    }
}
