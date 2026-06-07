using Lus.Contracts.Notifications;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;

namespace Lus.NotificationCenter.Services
{
    public interface INotificationCenterService
    {
        string GenerateMailTemplate(UserDto user, MailType mailType, AdditionalNotificationDataDto additionalData = null);

        string GenerateHtmlMailTemplate(string templateData);

        string GenerateSmsTemplate(UserDto user, SmsType smsType, AdditionalNotificationDataDto additionalData = null);

        Task SendMailNotificationAsync(MailNotificationDto mailNotificationDto, string replyToMail = "", Dictionary<string, byte[]> fileList = null);

        Task<string> SendSmsNotificationAsync(SmsNotificationDto smsNotificationDto);

        Task SendCalendarInviteAsync(CalendarNotificationDto calendarNotificationDto);

        Dictionary<string, byte[]> GetMailAttachments(string templateData);
    }
}
