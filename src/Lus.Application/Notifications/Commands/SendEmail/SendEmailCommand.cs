using MediatR;
using Lus.Contracts.Notifications;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;

namespace Lus.Application.Notifications.Commands.SendEmail
{
    public record SendEmailCommand(UserDto User, MailType MailType, AdditionalNotificationDataDto additionalData = null) : IRequest<Unit>;
}