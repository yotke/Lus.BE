namespace Lus.Contracts.Auth
{
    /// <summary>
    /// Self-service registration payload for the public endpoint
    /// (POST /api/auth/register). The created user is unconfirmed until they
    /// follow the confirmation link sent by e-mail.
    /// </summary>
    public class RegisterRequestDto
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Phone { get; set; }

        public string IdNumber { get; set; }
    }
}
