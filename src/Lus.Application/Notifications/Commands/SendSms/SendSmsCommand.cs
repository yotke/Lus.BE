using MediatR;
using Lus.Contracts.Notifications;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;

namespace Lus.Application.Notifications.Commands.SendSms
{
    public record SendSmsCommand(UserDto User, SmsType SmsType, AdditionalNotificationDataDto additionalData = null) : IRequest<Unit>;
}