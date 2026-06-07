using Lus.Contracts.Notifications;

namespace Lus.NotificationCenter.CalendarServices
{
    public interface ICalendarService
    {
        Task SendCalendarInviteAsync(CalendarNotificationDto calendarNotificationDto);
    }
}
