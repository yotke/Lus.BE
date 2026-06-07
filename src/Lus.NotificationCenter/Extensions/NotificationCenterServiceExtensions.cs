using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lus.NotificationCenter.CalendarServices;
using Lus.NotificationCenter.EmailServices;
using Lus.NotificationCenter.Services;
using Lus.NotificationCenter.SmsServices;
using Lus.NotificationCenter.TemplateServices;

namespace Lus.NotificationCenter.Extensions
{
    public static class NotificationCenterServiceExtensions
    {
        public static IServiceCollection AddNotificationCenter(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<INotificationCenterService, NotificationCenterService>();
            services.AddSingleton<IEmailService, EmailService>();
            services.AddSingleton<ITemplateService, TemplateService>();
            services.AddSingleton<ICalendarService, CalendarService>();
            services.AddSingleton<ISmsService, SmsService>();

            return services;
        }
    }
}
