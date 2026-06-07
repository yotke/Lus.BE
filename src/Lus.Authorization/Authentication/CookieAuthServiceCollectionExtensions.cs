using Lus.Authorization.Csrf;
using Microsoft.Extensions.DependencyInjection;

namespace Lus.Authorization.Authentication
{
    public static class CookieAuthServiceCollectionExtensions
    {
        public static IServiceCollection AddCookieAuthRuntimeServices(this IServiceCollection services)
        {
            services.AddScoped<ICookieAuthSessionService, CookieAuthSessionService>();
            services.AddScoped<ICsrfTokenService, CsrfTokenService>();
            return services;
        }
    }
}
