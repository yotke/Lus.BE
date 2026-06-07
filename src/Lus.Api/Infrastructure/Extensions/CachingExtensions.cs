using Newtonsoft.Json;

namespace Lus.Infrastructure.Extensions
{
    public static class CachingExtensions
    {
        public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddEasyCaching(options =>
            {
                var cacheProvider = configuration.GetValue<string>("Caching:ProviderName");
                options.UseInMemory(cacheProvider);
                options.WithJson(jsonSerializerSettingsConfigure: json => json.ReferenceLoopHandling = ReferenceLoopHandling.Ignore, "json");
            });

            return services;
        }
    }
}
