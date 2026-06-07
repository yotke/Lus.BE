using AutoMapper;
using Lus.Application.Notifications.Commands.SendHtmlTemplate;
using Lus.Application.Notifications.Entities;
using Lus.Contracts.HtmlTemplates;
using Lus.Contracts.Notifications;

namespace Lus.Application.Notifications
{
    public class NotificationMappingProfile : Profile
    {
        public NotificationMappingProfile()
        {
            CreateMap<MailNotification, MailNotificationDto>();
            CreateMap<SmsNotification, SmsNotificationDto>();
            CreateMap<HtmlTemplateNotificationDto, SendHtmlTemplateCommand>();
        }
    }
}
