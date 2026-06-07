using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Lus.Application.Common.Ports;
using Lus.Contracts.SystemTables;
using Lus.Infrastructure.Adapters.Recaptcha;
using Lus.Infrastructure.Extensions;

namespace Lus.Infrastructure.Adapters.SystemWS
{
    public class SystemWSAdapter : ISystemWSAdapter
    {
        private readonly HttpClient client;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<RecaptchaAdapter> logger;

        public SystemWSAdapter(HttpClient client, IHttpContextAccessor httpContextAccessor, ILogger<RecaptchaAdapter> logger)
        {
            this.client = client;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
        }

        public async Task<List<SystemCityDto>> GetSystemCitiesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var requestResponse = await this.client.GetAsync("GetCityStreet?getType=true", cancellationToken);

                if (requestResponse.IsSuccessStatusCode)
                {
                    var response = await requestResponse.Content.ReadAsStringAsync(cancellationToken);
                    return response.AsModel<List<SystemCityDto>>();
                }
                else
                {
                    return new List<SystemCityDto>();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception");
                return new List<SystemCityDto>();
            }
        }

        public async Task<List<SystemStreetDto>> GetSystemStreetsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var requestResponse = await this.client.GetAsync("GetCityStreet?getType=false", cancellationToken);

                if (requestResponse.IsSuccessStatusCode)
                {
                    var response = await requestResponse.Content.ReadAsStringAsync(cancellationToken);
                    return response.AsModel<List<SystemStreetDto>>();

                }
                else
                {
                    return new List<SystemStreetDto>();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Exception");
                return new List<SystemStreetDto>();
            }
        }
    }
}
