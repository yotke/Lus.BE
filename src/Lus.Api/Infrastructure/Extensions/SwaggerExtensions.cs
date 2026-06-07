using Microsoft.OpenApi.Models;

namespace Lus.Infrastructure.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Lus API", Version = "v1" });

                // Cookie-based auth: the SPA logs in via POST /api/auth/login and the browser
                // sends the auth cookie automatically. A Bearer definition is still exposed for
                // service-to-service callers that use the BasicAuthentication scheme.
                const string securityDefinitionId = "bearer";
                var securityScheme = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = securityDefinitionId },
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Service-to-service Bearer token (optional). The first-party UI uses cookies."
                };

                c.AddSecurityDefinition(securityDefinitionId, securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securityScheme, Array.Empty<string>() }
                });
            });

            return services;
        }
    }
}
