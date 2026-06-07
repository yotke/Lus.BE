namespace Lus.Application.Common.Ports
{
    /// <summary>
    /// Verifies a Google ID token (the credential returned by Google Identity
    /// Services on the client) and returns the verified user profile.
    /// </summary>
    public interface IGoogleSignInAdapter
    {
        Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// The subset of the Google profile we rely on after verifying the token.
    /// </summary>
    public class GoogleUserInfo
    {
        public string Subject { get; set; }

        public string Email { get; set; }

        public bool EmailVerified { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}
