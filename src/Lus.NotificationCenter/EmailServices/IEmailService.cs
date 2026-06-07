using Lus.Contracts.Notifications;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;

namespace Lus.NotificationCenter.EmailServices
{
    public interface IEmailService
    {
        string GetEmailTemplate(MailType mailType);

        string GenerateMailByType(UserDto user, MailType mailType, string mailTemplate, AdditionalNotificationDataDto additionalNotificationData = null);

        Task<bool> SendMailAsync(MailNotificationDto mailNotificationDto, string replyToMail = "", Dictionary<string, byte[]> fileList = null);
    }
}
