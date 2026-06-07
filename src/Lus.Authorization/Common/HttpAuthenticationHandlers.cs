using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Lus.Contracts;

namespace Lus.Authorization.Common
{
    public static class HttpAuthenticationHandlers
    {
        public static async Task AuthenticationChallenge(JwtBearerChallengeContext context, string errorCode)
        {
            // Override the response status code.
            context.Response.StatusCode = 401;

            // Emit the WWW-Authenticate header.
            context.Response.Headers.Append(HeaderNames.WWWAuthenticate, context.Options.Challenge);

            var errorModel = new ErrorModel
            {
                Code = errorCode
            };

            if (context.Error == null)
            {
                errorModel.Message = "Authentication failed";
            }
            else if (!string.IsNullOrEmpty(context.ErrorDescription))
            {
                errorModel.Message = context.ErrorDescription;
            }
            else
            {
                errorModel.Message = context.Error;
            }

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(errorModel), Encoding.UTF8);

            context.HandleResponse();
        }
    }
}
