using Lus.Contracts.Organizations;
using Lus.Contracts.Roles;
using Newtonsoft.Json;

namespace Lus.Infrastructure.IdentityServer.Models
{
    public class TokenResultDto
    {
        [JsonProperty(PropertyName = "id_token")]
        public string IdToken { get; set; }

        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty(PropertyName = "scope")]
        public string Scope { get; set; }

        [JsonProperty(PropertyName = "first_name")]
        public string FirstName { get; set; }

        [JsonProperty(PropertyName = "phone")]
        public string Phone { get; set; }

        [JsonProperty(PropertyName = "id_number")]
        public string IdNumber { get; set; }

        [JsonProperty(PropertyName = "last_name")]
        public string LastName { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "roles")]
        public ICollection<AuthRoleDto> Roles { get; set; }

        [JsonProperty(PropertyName = "organizations")]
        public ICollection<AuthOrganizationDto> Organizations { get; set; }

        [JsonExtensionData]
        [JsonProperty(PropertyName = "custom")]
        public Dictionary<string, object> Custom { get; set; }
    }

    public class ErrorTokenResultDto
    {
        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }

        [JsonProperty(PropertyName = "error_description")]
        public string ErrorDescription { get; set; }

        [JsonExtensionData]
        [JsonProperty(PropertyName = "custom")]
        public Dictionary<string, object> Custom { get; set; }
    }
}
