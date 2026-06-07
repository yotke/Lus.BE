using Lus.Contracts.Notifications.Types;

namespace Lus.Contracts.Notifications
{
    public class SmsNotificationDto
    {
        public int Id { get; set; }

        public string PhoneNumber { get; set; }

        public string Message { get; set; }

        public string Response { get; set; }

        public SmsType? SmsType { get; set; }

        public int? UserId { get; set; }
    }
}
