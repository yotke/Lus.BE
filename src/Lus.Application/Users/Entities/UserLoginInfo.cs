using Lus.Application.Common;

namespace Lus.Application.Users.Entities
{
    public class UserLoginInfo : EntityBase<int>
    {
        public int? UserId { get; set; }

        public User User { get; set; }

        public string UserAgent { get; set; }

        public string IpAddress { get; set; }
    }
}
