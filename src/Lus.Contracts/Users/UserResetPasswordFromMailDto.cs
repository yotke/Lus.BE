namespace Lus.Contracts.Users
{
    public class UserResetPasswordFromMailDto
    {
        public string PasswordVerificationToken { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }
    }
}
