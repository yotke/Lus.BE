namespace Lus.Contracts.Users
{
    public class UserResetPasswordFromSmsDto
    {
        public string Email { get; set; }

        public string SmsCode { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }
    }
}
