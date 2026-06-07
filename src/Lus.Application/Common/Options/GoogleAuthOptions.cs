namespace Lus.Application.Common.Options
{
    /// <summary>
    /// Google Sign-In configuration. The <see cref="ClientId"/> is the OAuth 2.0
    /// Web client id from the Google Cloud console. The id token returned by
    /// Google Identity Services must have this value in its <c>aud</c> claim.
    /// </summary>
    public class GoogleAuthOptions
    {
        public string ClientId { get; set; }

        public bool IsDebugMode { get; set; }
    }
}
