using Lus.Application.Common;
using Lus.Application.Users.Entities;
using Lus.Contracts.Notifications.Types;

namespace Lus.Application.Notifications.Entities
{
    public class MailNotification : EntityBase<int>
    {
        public string SenderEmail { get; set; }

        public string RecepientEmail { get; set; }

        public string Subject { get; set; }

        public int? UserId { get; set; }

        public User User { get; set; }

        public string Phone { get; set; }

        public bool IncludingFiles { get; set; }

        public MailType? MailType { get; set; }

        public string FreeText { get; set; }
    }
}
