using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Options;
using Lus.Application.Common.Ports;
using Lus.Contracts.Common.Ports;
using Lus.Infrastructure.Exceptions;
using Lus.Infrastructure.Extensions;
using System.Web;

namespace Lus.Infrastructure.Adapters.Recaptcha
{
    public class RecaptchaAdapter : IRecaptchaAdapter
    {
        private readonly HttpClient client;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly RecaptchaConfigOptions recaptchaConfigOptions;
        private readonly ILogger<RecaptchaAdapter> logger;

        public RecaptchaAdapter(HttpClient client, IOptions<RecaptchaConfigOptions> options, IHttpContextAccessor httpContextAccessor, ILogger<RecaptchaAdapter> logger)
        {
            this.client = client;
            this.httpContextAccessor = httpContextAccessor;
            this.recaptchaConfigOptions = options.Value;
            this.logger = logger;
        }

        public async Task<bool> CheckRecaptcha(CancellationToken cancellationToken = default)
        {
            try
            {
                if (this.recaptchaConfigOptions.IsDebugMode)
                {
                    return true;
                }

                var recaptchaResponse = this.httpContextAccessor.HttpContext?.Request.Headers["Recaptcha"];

                this.logger.LogInformation(HttpUtility.UrlPathEncode($"secret={this.recaptchaConfigOptions.RecaptchaSecretKey}&response={recaptchaResponse}&remoteip={GetIPAddresses()}"));
                var requestResponse = await this.client.GetAsync(
                    "?" + HttpUtility.UrlPathEncode($"secret={this.recaptchaConfigOptions.RecaptchaSecretKey}&response={recaptchaResponse}&remoteip={GetIPAddresses()}"), cancellationToken);

                if (requestResponse.IsSuccessStatusCode)
                {
                    var response = await requestResponse.Content.ReadAsStringAsync(cancellationToken);
                    var recaptchaDto = response.AsModel<RecaptchaDto>();

                    if (!recaptchaDto.success || (recaptchaDto.score < (decimal)0.5))
                    {
                        this.logger.LogError("Recaptcha Response error", recaptchaDto);
                        throw new MembershipException(41);
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    throw new MembershipException(41);
                }
            }
            catch (RecaptchaRequestException ex)
            {
                this.logger.LogError(ex, "RecaptchaRequestException");
                throw new MembershipException(50);

            }
            catch (HttpRequestException ex)
            {
                if (ex.GetType() == typeof(MembershipException))
                    throw ex;
                this.logger.LogError(ex, "HttpRequestException");
                return true;
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(MembershipException))
                    throw ex;
                this.logger.LogError(ex, "Exception");
                return true;
            }
        }

        private string GetIPAddresses()
        {
            string clientIp = this.httpContextAccessor.HttpContext?.GetServerVariable("HTTP_X_FORWARDED_FOR");
            if (string.IsNullOrEmpty(clientIp))
                clientIp = this.httpContextAccessor.HttpContext?.GetServerVariable("REMOTE_ADDR");

            if (string.IsNullOrEmpty(clientIp))
            {
                clientIp = "::1";
            }
            return clientIp;
        }
    }
}
