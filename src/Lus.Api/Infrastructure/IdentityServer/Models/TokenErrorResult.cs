using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using Lus.Infrastructure.Common;

namespace Lus.Infrastructure.IdentityServer.Models
{
    public class TokenErrorResult : IEndpointResult
    {
        public TokenErrorResponse Response { get; }

        public TokenErrorResult()
        {
        }

        public TokenErrorResult(TokenErrorResponse error)
        {
            if (string.IsNullOrWhiteSpace(error.Error))
            {
                throw new ArgumentNullException(nameof(error.Error), "Error must be set");
            }

            Response = error;
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            context.Response.StatusCode = 400;
            context.Response.SetNoCache();

            var dto = new ErrorTokenResultDto
            {
                Error = Response.Error,
                ErrorDescription = Response.ErrorDescription,

                Custom = Response.Custom
            };

            await context.WriteJsonAsync(dto);
        }
    }
}
