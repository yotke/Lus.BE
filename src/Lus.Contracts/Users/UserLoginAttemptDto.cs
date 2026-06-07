using Lus.Contracts.Users.Types;

namespace Lus.Contracts.Users
{
    public class UserLoginAttemptDto
    {
        public int Id { get; set; }

        public string UserName { get; set; }

        public LoginFailReasonType? LoginFailReason { get; set; }

        public UserLoginAttemptType UserLoginAttemptType { get; set; }

        public int? UserId { get; set; }
    }
}
