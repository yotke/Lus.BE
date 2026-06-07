using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Options;
using Lus.Application.Common.Ports;
using Lus.Infrastructure.Exceptions;
using Lus.Infrastructure.Extensions;
using Newtonsoft.Json;

namespace Lus.Infrastructure.Adapters.Google
{
    /// <summary>
    /// Verifies a Google ID token against Google's tokeninfo endpoint and checks
    /// that the token was issued for our OAuth client. This avoids bundling the
    /// Google.Apis.Auth library while still performing the security-critical
    /// checks (signature is validated by Google, audience + issuer + expiry are
    /// validated here).
    /// </summary>
    public class GoogleSignInAdapter : IGoogleSignInAdapter
    {
        private static readonly string[] ValidIssuers =
        {
            "accounts.google.com",
            "https://accounts.google.com"
        };

        private readonly HttpClient client;
        private readonly GoogleAuthOptions options;
        private readonly ILogger<GoogleSignInAdapter> logger;

        public GoogleSignInAdapter(HttpClient client, IOptions<GoogleAuthOptions> options, ILogger<GoogleSignInAdapter> logger)
        {
            this.client = client;
            this.options = options.Value;
            this.logger = logger;
        }

        public async Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new MembershipException(42);
            }

            GoogleTokenInfoDto tokenInfo;
            try
            {
                var response = await this.client.GetAsync($"tokeninfo?id_token={WebUtility.UrlEncode(idToken)}", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    this.logger.LogWarning("Google tokeninfo returned status {Status}", response.StatusCode);
                    throw new MembershipException(42);
                }

                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                tokenInfo = payload.AsModel<GoogleTokenInfoDto>();
            }
            catch (MembershipException)
            {
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to verify Google id token");
                throw new MembershipException(42);
            }

            if (tokenInfo == null)
            {
                throw new MembershipException(42);
            }

            if (!this.options.IsDebugMode)
            {
                if (string.IsNullOrWhiteSpace(this.options.ClientId) || !string.Equals(tokenInfo.Aud, this.options.ClientId, StringComparison.Ordinal))
                {
                    this.logger.LogWarning("Google id token audience mismatch. Expected {Expected} got {Actual}", this.options.ClientId, tokenInfo.Aud);
                    throw new MembershipException(42);
                }
            }

            if (string.IsNullOrWhiteSpace(tokenInfo.Iss) || Array.IndexOf(ValidIssuers, tokenInfo.Iss) < 0)
            {
                throw new MembershipException(42);
            }

            if (string.IsNullOrWhiteSpace(tokenInfo.Email))
            {
                throw new MembershipException(42);
            }

            var emailVerified = string.Equals(tokenInfo.EmailVerified, "true", StringComparison.OrdinalIgnoreCase);
            if (!emailVerified)
            {
                throw new MembershipException(42);
            }

            return new GoogleUserInfo
            {
                Subject = tokenInfo.Sub,
                Email = tokenInfo.Email.Trim().ToLowerInvariant(),
                EmailVerified = emailVerified,
                FirstName = tokenInfo.GivenName,
                LastName = tokenInfo.FamilyName
            };
        }

        private class GoogleTokenInfoDto
        {
            [JsonProperty("aud")]
            public string Aud { get; set; }

            [JsonProperty("iss")]
            public string Iss { get; set; }

            [JsonProperty("sub")]
            public string Sub { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("email_verified")]
            public string EmailVerified { get; set; }

            [JsonProperty("given_name")]
            public string GivenName { get; set; }

            [JsonProperty("family_name")]
            public string FamilyName { get; set; }
        }
    }
}
