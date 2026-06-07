using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Notifications;
using Lus.Contracts.Users;

namespace Lus.NotificationCenter.SmsServices
{
    public interface ISmsService
    {
        string GenerateStringCode();

        string GetSmsTemplate(UserDto user, SmsType typeOfNotification, string alternativeMsg = null, AdditionalNotificationDataDto additionalData = null);

        Task<string> SendSmsAsync(SmsNotificationDto smsNotificationDto, bool isError = false);
    }
}
