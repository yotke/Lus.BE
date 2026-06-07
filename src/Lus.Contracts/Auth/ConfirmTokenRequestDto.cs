namespace Lus.Contracts.Auth
{
    /// <summary>
    /// Confirms a user via the email confirmation token and signs them in.
    /// Replaces the IdentityServer4 "confirm_token" extension grant.
    /// </summary>
    public class ConfirmTokenRequestDto
    {
        public string ConfirmToken { get; set; }
    }
}

