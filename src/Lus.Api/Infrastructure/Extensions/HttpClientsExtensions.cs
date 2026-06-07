using Lus.Application.Common.Ports;
using Lus.Infrastructure.Adapters.Recaptcha;
using Lus.Infrastructure.Adapters.SystemWS;

namespace Lus.Infrastructure.Extensions
{
    public static class HttpClientsExtensions
    {
        public static IServiceCollection AddHttpClientsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var recaptchaUrl = new Uri(configuration.GetValue<string>("RecaptchaSettings:RecaptchaUrl"));
            services.AddCustomHttpClient<IRecaptchaAdapter, RecaptchaAdapter>("RecaptchaAdapter", "RecaptchaAdapter", recaptchaUrl);

            var systemWSUrl = new Uri(configuration.GetValue<string>("SystemWSSetting:SystemUsersProviderUrl"));
            services.AddCustomHttpClient<ISystemWSAdapter, SystemWSAdapter>("SystemWSAdapter", "SystemWSAdapter", systemWSUrl);

            return services;
        }

        private static IServiceCollection AddCustomHttpClient<TInterface, T>(this IServiceCollection services,
            string clientName,
            string serviceName,
            Uri uri)
            where TInterface : class
            where T : class, TInterface
        {
            if (uri != null)
            {
                services.AddHttpClient<TInterface, T>(clientName, (sp, client) =>
                {
                    client.BaseAddress = uri;
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("User-Agent", serviceName);
                });
            }

            return services;
        }
    }
}
