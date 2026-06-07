using MediatR;
using Lus.Contracts.Notifications;

namespace Lus.Application.Notifications.Commands.SendCalendar
{
    public record SendCalendarCommand(CalendarNotificationDto CalendarNotification) : IRequest<Unit>;
}
