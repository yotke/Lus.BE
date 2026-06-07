namespace Lus.Contracts.Auth
{
    /// <summary>
    /// Credentials for the cookie login endpoint (POST /api/auth/login).
    /// Replaces the IdentityServer4 resource-owner-password grant.
    /// </summary>
    public class LoginRequestDto
    {
        public string Email { get; set; }

        public string Password { get; set; }
    }
}

