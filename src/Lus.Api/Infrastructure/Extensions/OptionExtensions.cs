using Microsoft.AspNetCore.Mvc;
using Lus.Application;
using Lus.Application.Common.Options;
using Lus.Contracts.Options;
using Lus.Infrastructure.ErrorHandlers;
using Lus.Infrastructure.Options;

namespace Lus.Infrastructure.Extensions
{
    public static class OptionExtensions
    {
        public static IServiceCollection AddOptionsConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = ValidationHelpers.InvalidModelStateResponseFactory;
            });

            services.Configure<UserLoginAuditingOptions>(configuration.GetSection(ApplicationConstants.OptionSections.UserLoginAuditing))
                .Configure<PasswordConfigOptions>(configuration.GetSection(ApplicationConstants.OptionSections.PasswordConfigOptions))
                .Configure<MailNotificationOptions>(configuration.GetSection(ApplicationConstants.OptionSections.MailNotificationOptions))
                .Configure<ApplicationOptions>(configuration.GetSection(ApplicationConstants.OptionSections.ApplicationOptions))
                .Configure<SmsNotificationOptions>(configuration.GetSection(ApplicationConstants.OptionSections.SmsNotificationOptions))
                .Configure<RecaptchaConfigOptions>(configuration.GetSection(ApplicationConstants.OptionSections.RecaptchaConfigOptions))
                .Configure<GoogleAuthOptions>(configuration.GetSection(ApplicationConstants.OptionSections.GoogleAuthOptions));
            return services;
        }
    }
}
