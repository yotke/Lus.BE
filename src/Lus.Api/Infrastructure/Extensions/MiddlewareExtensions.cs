using Lus.Infrastructure.IdentityServer;
using System.Threading.RateLimiting;

namespace Lus.Infrastructure.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseIPFilter(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IPFilter>();
        }

        public static IServiceCollection AddRateLimiter(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    
                RateLimitPartition.GetFixedWindowLimiter(httpContext.GetIP(), partition =>
                        new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = configuration.GetValue<int>("ApplicationOptions:ActionsToStartSlow"),
                            QueueLimit = 10,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            Window = TimeSpan.FromSeconds(configuration.GetValue<int>("ApplicationOptions:MaxExecutionBlockAgeInMinutes"))
                        }));
            });

            return services;
        }

        private static string GetIP(this HttpContext context)
        {
            var clientIp = context?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(clientIp))
            {
                // X-Forwarded-For may contain a comma-separated list; take the first (original client).
                clientIp = clientIp.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault();
            }

            if (string.IsNullOrEmpty(clientIp))
            {
                clientIp = context?.Connection?.RemoteIpAddress?.ToString();
            }

            if (string.IsNullOrEmpty(clientIp))
            {
                clientIp = "::1";
            }
            return clientIp;
        }
    }
}
