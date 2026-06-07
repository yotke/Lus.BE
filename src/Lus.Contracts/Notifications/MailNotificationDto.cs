using Lus.Contracts.Notifications.Types;

namespace Lus.Contracts.Notifications
{
    public class MailNotificationDto
    {
        public int Id { get; set; }

        public string SenderEmail { get; set; }

        public string RecepientEmail { get; set; }

        public string Subject { get; set; }

        public int? UserId { get; set; }

        public string Phone { get; set; }

        public string DisplayName { get; set; }

        public bool IncludingFiles { get; set; }

        public MailType? MailType { get; set; }

        public string FreeText { get; set; }
    }
}
