using Lus.Contracts.Users.Types;

namespace Lus.Contracts.Users
{
    public class UserResetPasswordDto
    {
        public string Email { get; set; }

        public ResetPasswordType ResetPasswordType { get; set; }
    }
}
