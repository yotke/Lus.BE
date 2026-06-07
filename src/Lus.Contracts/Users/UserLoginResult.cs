using Lus.Contracts.Users.Types;

namespace Lus.Contracts.Users
{
    public class UserLoginResult
    {
        public bool UserFound { get; set; } = false;

        public bool IsValidCredentials { get; set; } = false;

        public bool IsConfirmed { get; set; } = false;

        public int? UserId { get; set; }

        public string ErrorMessage { get; set; }

        public LoginFailReasonType? LoginFailReason { get; set; } = null;

        public double? LockTimeLeft { get; set; } = 0;
    }
}
