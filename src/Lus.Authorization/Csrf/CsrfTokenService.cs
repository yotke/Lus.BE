using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Lus.Authorization.Csrf
{
    public interface ICsrfTokenService
    {
        string GetAndStoreToken(HttpContext httpContext);
    }

    public sealed class CsrfTokenService : ICsrfTokenService
    {
        private readonly IAntiforgery antiforgery;

        public CsrfTokenService(IAntiforgery antiforgery) => this.antiforgery = antiforgery;

        public string GetAndStoreToken(HttpContext httpContext)
        {
            var tokens = this.antiforgery.GetAndStoreTokens(httpContext);
            return tokens.RequestToken ?? string.Empty;
        }
    }
}
