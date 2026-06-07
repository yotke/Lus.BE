namespace Lus.Authorization.Common
{
    public class ServiceClientConfiguration
    {
        public Uri Url { get; set; }

        public string ApiScope { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
    }
}
