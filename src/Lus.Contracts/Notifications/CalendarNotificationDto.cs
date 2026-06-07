using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;

namespace Lus.Contracts.Notifications
{
    public class CalendarNotificationDto
    {
        public DateTime StartDate { get; set; }

        public int Interval { get; set; }

        public UserInfoDto User { get; set; }

        public string TenderName { get; set; }

        public string CommitteeName { get; set; }

        public string SummonAddress { get; set; }

        public string OrganizationName { get; set; }

        public string SendUserEmail { get; set; }

        public CalendarMethodTypes CalendarMethod { get; set; }
    }
}
