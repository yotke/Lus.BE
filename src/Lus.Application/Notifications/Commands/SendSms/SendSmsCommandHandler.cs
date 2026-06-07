using AutoMapper;
using MediatR;
using Lus.Application.Notifications.Entities;
using Lus.Application.Notifications.Repositories;
using Lus.Contracts.Notifications;
using Lus.NotificationCenter.Services;

namespace Lus.Application.Notifications.Commands.SendSms
{
    public class SendSmsCommandHandler : IRequestHandler<SendSmsCommand, Unit>
    {
        private readonly ISmsNotificationsRepository smsNotificationsRepository;
        private readonly IMapper mapper;
        private readonly INotificationCenterService notificationCenterService;

        public SendSmsCommandHandler(ISmsNotificationsRepository smsNotificationsRepository, IMapper mapper, INotificationCenterService notificationCenterService)
        {
            this.notificationCenterService = notificationCenterService;
            this.smsNotificationsRepository = smsNotificationsRepository;
            this.mapper = mapper;
        }

        public async Task<Unit> Handle(SendSmsCommand command, CancellationToken cancellationToken)
        {
            var smsNotification = CreateSmsNotification(command);

            smsNotification = await this.smsNotificationsRepository.AddAsync(smsNotification, cancellationToken);

            var smsNotificationDto = this.mapper.Map<SmsNotificationDto>(smsNotification);

            smsNotification.Response = await this.notificationCenterService.SendSmsNotificationAsync(smsNotificationDto);

            await this.smsNotificationsRepository.UpdateAsync(smsNotification, cancellationToken);

            return Unit.Value;
        }

        private SmsNotification CreateSmsNotification(SendSmsCommand command)
        {
            var smsNotification = new SmsNotification
            {
                SmsType = command.SmsType,
                Message = this.notificationCenterService.GenerateSmsTemplate(command.User, command.SmsType, command.additionalData),
                UserId = command.User.Id,
                PhoneNumber = command.User.Phone
            };

            return smsNotification;
        }
    }
}
