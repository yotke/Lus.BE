using MediatR;
using Lus.NotificationCenter.Services;

namespace Lus.Application.Notifications.Commands.SendCalendar
{
    public class SendCalendarCommandHandler : IRequestHandler<SendCalendarCommand, Unit>
    {
        private readonly INotificationCenterService notificationCenterService;


        public SendCalendarCommandHandler(INotificationCenterService notificationCenterService)
        {
            this.notificationCenterService = notificationCenterService;
        }

        public async Task<Unit> Handle(SendCalendarCommand command, CancellationToken cancellationToken)
        {
            await this.notificationCenterService.SendCalendarInviteAsync(command.CalendarNotification);

            return Unit.Value;
        }
    }
}
