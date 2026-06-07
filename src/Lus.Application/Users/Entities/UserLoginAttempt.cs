using Lus.Application.Common;
using Lus.Contracts.Users.Types;

namespace Lus.Application.Users.Entities
{
    public class UserLoginAttempt : EntityBase<int>
    {
        public string UserName { get; set; }

        public LoginFailReasonType? LoginFailReason { get; set; }

        public UserLoginAttemptType UserLoginAttemptType { get; set; }

        public int? UserId { get; set; }

        public User User { get; set; }
    }
}
