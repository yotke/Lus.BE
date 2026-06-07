using AutoMapper;
using MediatR;
using Microsoft.Extensions.Options;
using Lus.Application.Notifications.Entities;
using Lus.Application.Notifications.Repositories;
using Lus.Contracts.Notifications;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Options;
using Lus.NotificationCenter.Services;

namespace Lus.Application.Notifications.Commands.SendEmail
{
    public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, Unit>
    {
        private readonly IMailNotificationsRepository mailNotificationsRepository;
        private readonly IMapper mapper;
        private readonly INotificationCenterService notificationCenterService;
        private readonly MailNotificationOptions mailNotificationOptions;
        private readonly Dictionary<MailType, string> MailSubjectsDic = new Dictionary<MailType, string>
        {
            { MailType.SiteRegistration, "אימות אימייל" },
            { MailType.LogInWithoutPassword, "אימות אימייל" },
            { MailType.PasswordReset, "איפוס סיסמה" },
            { MailType.ApplicationModified, "שינוי הגשת מועמדות בעקבות דחייה או אישור קובץ על ידי מנהל המכרז" },
            { MailType.ApplicationConfirmation, "אישור הגשת מועמדות" },
            { MailType.MembersSignatureMail, "חתימת חבר ועדה" },
            { MailType.MembersProtocolMail, "פרוטוקול ועדה" },
            { MailType.UserLoginInfoNotification, "זוהתה כניסה ממכשיר אחר" }
        };


        public SendEmailCommandHandler(IOptions<MailNotificationOptions> options, IMailNotificationsRepository mailNotificationsRepository, IMapper mapper, INotificationCenterService notificationCenterService)
        {
            this.mailNotificationOptions = options.Value;
            this.notificationCenterService = notificationCenterService;
            this.mailNotificationsRepository = mailNotificationsRepository;
            this.mapper = mapper;
        }

        public async Task<Unit> Handle(SendEmailCommand command, CancellationToken cancellationToken)
        {
            var mailNotification = CreateMailNotification(command);

            mailNotification = await this.mailNotificationsRepository.AddAsync(mailNotification, cancellationToken);

            var mailNotificationDto = this.mapper.Map<MailNotificationDto>(mailNotification);
            if (command.additionalData?.fileList != null)
            {
                await this.notificationCenterService.SendMailNotificationAsync(mailNotificationDto, null, command.additionalData.fileList);
            }
            else
            {
                await this.notificationCenterService.SendMailNotificationAsync(mailNotificationDto);
            }

            return Unit.Value;
        }

        private MailNotification CreateMailNotification(SendEmailCommand command)
        {
            var mailNotification = new MailNotification
            {
                SenderEmail = ApplicationConstants.NotificationConstants.MailSender,
                RecepientEmail = !string.IsNullOrWhiteSpace(this.mailNotificationOptions.DebugEmails) ? this.mailNotificationOptions.DebugEmails : command.User.Email,
                UserId = command.User.Id,
                Subject = this.MailSubjectsDic[command.MailType],
                Phone = command.User.Phone,
                FreeText = this.notificationCenterService.GenerateMailTemplate(command.User, command.MailType, command.additionalData),
                MailType = command.MailType
            };

            return mailNotification;
        }
    }
}
