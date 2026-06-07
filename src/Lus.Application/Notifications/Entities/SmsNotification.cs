using Lus.Application.Common;
using Lus.Application.Users.Entities;
using Lus.Contracts.Notifications.Types;

namespace Lus.Application.Notifications.Entities
{
    public class SmsNotification : EntityBase<int>
    {
        public string PhoneNumber { get; set; }

        public string Message { get; set; }

        public string Response { get; set; }

        public SmsType? SmsType { get; set; }

        public int? UserId { get; set; }

        public User User { get; set; }
    }
}
