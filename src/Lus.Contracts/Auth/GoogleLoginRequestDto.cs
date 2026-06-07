namespace Lus.Contracts.Auth
{
    /// <summary>
    /// Payload for Google Sign-In. <see cref="IdToken"/> is the credential
    /// (a JWT) returned by Google Identity Services on the client.
    /// </summary>
    public class GoogleLoginRequestDto
    {
        public string IdToken { get; set; }
    }
}
