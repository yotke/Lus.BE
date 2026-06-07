using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Lus.Application.Users.Validators;
using Lus.Infrastructure.ErrorHandlers;
using Lus.Infrastructure.SignalRHubs;

namespace Lus.Infrastructure.Extensions
{
    public static class EndpointsExtensions
    {
        public static void WithSwaggerRedirection(this IEndpointRouteBuilder endpoints, IConfiguration configuration) =>
            endpoints.MapGet("/", context =>
            {
                if (!configuration.GetValue<bool>("General:IsProduction"))
                {
                    context.Response.Redirect("/swagger/index.html");
                }

                return Task.CompletedTask;
            });

        public static IServiceCollection AddControllersWithConfiguration(this IServiceCollection services)
        {
            services
                .AddControllers(options =>
                {
                    options.Filters.Add<DomainExceptionFilter>();

                    options.Filters.Add<ValidateModelStateAttribute>();
                })
                .AddNewtonsoftJson(SerializationSettings.DefaultApiJsonOptions);

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
                options.SuppressConsumesConstraintForFormFileParameters = true;
            });
            services.AddMvc()
                .AddSessionStateTempDataProvider();
            services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters()
                .AddValidatorsFromAssemblyContaining<CreateUserValidator>();
            return services;
        }


        public static void MapTwoFactorAuthRedirection(this IEndpointRouteBuilder endpoints) =>
            endpoints.MapPost("connect/token", context =>
            {
                context.Response.Redirect("/connect/tokenAuth", true, true);
                return Task.CompletedTask;
            });

        public static void MapSignalRHubs(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<CitiesStreetsHub>("/citiesStreetsHub", options =>
                {
                    options.Transports =
                        HttpTransportType.WebSockets |
                        HttpTransportType.LongPolling;
                });
        }
    }
}
