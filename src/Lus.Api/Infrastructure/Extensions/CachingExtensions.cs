using EasyCaching.Core.Configurations;
using Lus.Application.Common.Services;
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
                // Builder sessions carry System.Text.Json JsonElements in their undo history;
                // plain Newtonsoft turns those into ValueKind.Undefined on the way back, and
                // the next write throws. CacheSerializerSettings adds the converter that keeps
                // them intact (see SessionRoundTripTests).
                options.WithJson(
                    jsonSerializerSettingsConfigure: json => CacheSerializerSettings.Apply(json),
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
                        // Bind the provider to OUR serializer explicitly. Leaving it implicit
                        // means whichever serializer happens to be registered wins, and the
                        // JsonElement converter is not optional — without it a round-tripped
                        // session throws on its next write.
                        config.SerializerName = "json";
                    }, "default");
                }
                else
                {
                    options.UseInMemory(config =>
                    {
                        // Deep-clone-on-read round-trips the value through a serializer, which
                        // is the same JsonElement hazard as Redis. Drafts are already cloned
                        // explicitly by DraftPatcher before mutation, so the extra copy buys
                        // nothing and costs correctness.
                        config.DBConfig.EnableReadDeepClone = false;
                        config.DBConfig.EnableWriteDeepClone = false;
                    }, "default");
                }
            });

            return services;
        }
    }
}
