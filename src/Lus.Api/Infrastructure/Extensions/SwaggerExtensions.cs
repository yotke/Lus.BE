using Microsoft.OpenApi.Models;

namespace Lus.Infrastructure.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Lus API", Version = "v1" });

                var requiredScope = configuration.GetValue<string>("Services:Identity:ApiScope");
                var securityDefinitionId = "oath2ClientCredentials";

                var securityScheme = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = securityDefinitionId },
                    Type = SecuritySchemeType.OAuth2,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Flows = new OpenApiOAuthFlows
                    {
                        ClientCredentials = new OpenApiOAuthFlow
                        {
                            TokenUrl = new Uri($"{configuration.GetValue<string>("Services:Identity:ExternalUrl")}/connect/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                { requiredScope, "For accessing the API at all" }
                            }
                        },
                        Password = new OpenApiOAuthFlow()
                        {
                            TokenUrl = new Uri($"{configuration.GetValue<string>("Services:Identity:ExternalUrl")}/connect/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                { requiredScope, "For accessing the API at all" }
                            }
                        }
                    }
                };

                c.AddSecurityDefinition(securityDefinitionId, securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {securityScheme, new string[] { requiredScope }}
                });
            });

            return services;
        }
    }
}
