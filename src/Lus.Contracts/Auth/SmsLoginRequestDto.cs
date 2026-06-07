namespace Lus.Contracts.Auth
{
    /// <summary>
    /// Signs a user in using a one-time SMS verification token.
    /// Replaces the IdentityServer4 "login_by_token" extension grant.
    /// </summary>
    public class SmsLoginRequestDto
    {
        public string SmsCode { get; set; }
    }
}

