using EasyCaching.Core.Configurations;
using Newtonsoft.Json;

namespace Lus.Infrastructure.Extensions
{
    public static class CachingExtensions
    {
        public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
        {
            var cacheProvider = configuration.GetValue<string>("Caching:ProviderName") ?? "default";

            services.AddEasyCaching(options =>
            {
                options.WithJson(
                    jsonSerializerSettingsConfigure: json => json.ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    "json");

                // Always register as "default" so existing IEasyCachingProvider
                // injections keep resolving. ProviderName selects the backend only.
                if (string.Equals(cacheProvider, "redis", StringComparison.OrdinalIgnoreCase))
                {
                    var host = configuration.GetValue<string>("Redis:Host") ?? "127.0.0.1";
                    var port = configuration.GetValue("Redis:Port", 6379);
                    var username = configuration.GetValue<string>("Redis:Username");
                    var password = configuration.GetValue<string>("Redis:Password");
                    var ssl = configuration.GetValue("Redis:Ssl", false);

                    options.UseRedis(config =>
                    {
                        config.DBConfig.Endpoints.Add(new ServerEndPoint(host, port));
                        if (!string.IsNullOrWhiteSpace(username))
                            config.DBConfig.Username = username;
                        if (!string.IsNullOrWhiteSpace(password))
                            config.DBConfig.Password = password;
                        config.DBConfig.IsSsl = ssl;
                    }, "default");
                }
                else
                {
                    options.UseInMemory("default");
                }
            });

            return services;
        }
    }
}
