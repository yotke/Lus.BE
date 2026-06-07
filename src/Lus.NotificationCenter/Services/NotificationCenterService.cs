using Lus.Contracts.Notifications;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;
using Lus.NotificationCenter.CalendarServices;
using Lus.NotificationCenter.EmailServices;
using Lus.NotificationCenter.SmsServices;
using Lus.NotificationCenter.TemplateServices;

namespace Lus.NotificationCenter.Services
{
    public class NotificationCenterService : INotificationCenterService
    {
        private readonly ITemplateService templateService;
        private readonly IEmailService emailService;
        private readonly ISmsService smsService;
        private readonly ICalendarService calendarService;

        public NotificationCenterService(ITemplateService templateService, IEmailService emailService, ISmsService smsService, ICalendarService calendarService)
        {
            this.templateService = templateService;
            this.emailService = emailService;
            this.smsService = smsService;
            this.calendarService = calendarService;
        }

        public string GenerateMailTemplate(UserDto user, MailType mailType, AdditionalNotificationDataDto additionalData = null)
        {
            var mailTemplate = this.emailService.GetEmailTemplate(mailType);

            return this.emailService.GenerateMailByType(user, mailType, mailTemplate, additionalData);
        }

        public string GenerateHtmlMailTemplate(string templateData) => this.templateService.GenerateHtmlMailTemplate(templateData);

        public string GenerateSmsTemplate(UserDto user, SmsType smsType, AdditionalNotificationDataDto additionalData = null) => this.smsService.GetSmsTemplate(user, smsType, null, additionalData);

        public async Task SendMailNotificationAsync(MailNotificationDto mailNotificationDto, string replyToMail = "", Dictionary<string, byte[]> fileList = null) => await this.emailService.SendMailAsync(mailNotificationDto, replyToMail, fileList);

        public async Task<string> SendSmsNotificationAsync(SmsNotificationDto smsNotificationDto) => await this.smsService.SendSmsAsync(smsNotificationDto);

        public async Task SendCalendarInviteAsync(CalendarNotificationDto calendarNotificationDto) => await this.calendarService.SendCalendarInviteAsync(calendarNotificationDto);

        public Dictionary<string, byte[]> GetMailAttachments(string templateData) => this.templateService.GetMailAttachments(templateData);
    }
}