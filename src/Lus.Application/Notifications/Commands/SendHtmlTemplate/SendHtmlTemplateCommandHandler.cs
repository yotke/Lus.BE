using AutoMapper;
using MediatR;
using Microsoft.Extensions.Options;
using Lus.Application.Contacts.Entities;
using Lus.Application.Notifications.Entities;
using Lus.Application.Notifications.Repositories;
using Lus.Application.Organizations.Entities;
using Lus.Application.Organizations.Repositories;
using Lus.Contracts.Common.Models;
using Lus.Contracts.Notifications;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Options;
using Lus.Contracts.Users;
using Lus.NotificationCenter.Services;
using System.Text.RegularExpressions;

namespace Lus.Application.Notifications.Commands.SendHtmlTemplate
{
    public class SendHtmlTemplateCommandHandler : IRequestHandler<SendHtmlTemplateCommand, Unit>
    {
        private readonly IOrganizationsRepository organizationsRepository;
        private readonly IMailNotificationsRepository mailNotificationsRepository;
        private readonly IMapper mapper;
        private readonly INotificationCenterService notificationCenterService;
        private readonly MailNotificationOptions mailNotificationOptions;
        private readonly string fixImagesLink = "src=\"";
        private readonly string fixImages = "(src=\"Home.*?\")";

        public SendHtmlTemplateCommandHandler(IOrganizationsRepository organizationsRepository, IOptions<MailNotificationOptions> options, IMailNotificationsRepository mailNotificationsRepository, IMapper mapper, INotificationCenterService notificationCenterService)
        {

            this.organizationsRepository = organizationsRepository;
            this.mailNotificationOptions = options.Value;
            this.notificationCenterService = notificationCenterService;
            this.mailNotificationsRepository = mailNotificationsRepository;
            this.mapper = mapper;
        }

        public async Task<Unit> Handle(SendHtmlTemplateCommand command, CancellationToken cancellationToken)
        {
            Dictionary<string, byte[]> attachments = null;
            if (command.MailType == MailType.UserRecruitmentHistoryMail || command.MailType == MailType.PassLetterId
                || command.MailType == MailType.RejectionLetterId || command.MailType == MailType.RelinquishLetterId)
            {
                attachments = this.notificationCenterService.GetMailAttachments(command.TemplateData);

                var organization = await this.organizationsRepository.GetWithIncludeAsync(command.OrganizationId.Value,
                    cancellationToken, o => o.Contacts);

                command.TemplateData = SetOrganizationItem(organization, command.TemplateData);

                if (organization.Contacts?.Any() ?? false)
                {
                    command.TemplateData =
                        SetOrganizationContactItem(organization.Contacts.First(), command.TemplateData);
                }

                command.TemplateData = SetUserItem(command.User, command.TemplateData);

            }

            var mailNotification = CreateMailNotification(command);

            mailNotification = await this.mailNotificationsRepository.AddAsync(mailNotification, cancellationToken);

            var mailNotificationDto = this.mapper.Map<MailNotificationDto>(mailNotification);

            mailNotificationDto.DisplayName = command.Name;

            await this.notificationCenterService.SendMailNotificationAsync(mailNotificationDto, string.IsNullOrWhiteSpace(command.ReplayEmail) ? "onecity.co.il" : command.ReplayEmail, attachments);

            return Unit.Value;
        }

        private MailNotification CreateMailNotification(SendHtmlTemplateCommand command)
        {
            var mailNotification = new MailNotification
            {
                SenderEmail = ApplicationConstants.NotificationConstants.MailSender,
                RecepientEmail = !string.IsNullOrWhiteSpace(this.mailNotificationOptions.DebugEmails) ? this.mailNotificationOptions.DebugEmails : command.User.Email,
                UserId = command.User.Id,
                Subject = command.Subject,
                Phone = command.User.Phone,
                FreeText = this.notificationCenterService.GenerateHtmlMailTemplate(command.TemplateData),
                MailType = command.MailType
            };
            return mailNotification;
        }

        private string SetOrganizationItem(Organization organization, string templateData)
        {
            string find;
            foreach (PropsModel Kval in GetPropsAndValues(organization))
            {
                find = $"$Organizations.{Kval.Key}$";
                if (Kval.typeCode != TypeCode.DBNull && Kval.typeCode != TypeCode.Empty && Kval.typeCode != TypeCode.Object)
                    templateData = templateData.Replace(find, Kval.Value.ToString());
            }

            Match maches = Regex.Match(templateData, fixImages);
            while (maches != null && !string.IsNullOrWhiteSpace(maches.Value))
            {
                templateData = templateData.Replace(maches.Value, maches.Value.Replace(fixImagesLink,
                    $"{fixImagesLink}https:///{this.mailNotificationOptions.SiteDomain}/").Replace("//", "/"));
                maches = Regex.Match(templateData, fixImages);
            }
            return templateData;
        }

        private List<PropsModel> GetPropsAndValues(object model)
        {
            return model.GetType().GetProperties().ToList().Select(p => new PropsModel(p.Name, p.GetValue(model))).ToList();
        }

        private string SetOrganizationContactItem(Contact contact, string templateData)
        {
            string find;
            foreach (PropsModel Kval in GetPropsAndValues(contact))
            {
                find = $"$Contact.{Kval.Key}$";
                if (Kval.typeCode != TypeCode.DBNull && Kval.typeCode != TypeCode.Empty && Kval.typeCode != TypeCode.Object)
                    templateData = templateData.Replace(find, Kval.Value.ToString());
            }
            return templateData;
        }

        private string SetUserItem(UserInfoDto user, string templateData)
        {
            string find;
            foreach (PropsModel Kval in GetPropsAndValues(user))
            {
                find = $"$User.{Kval.Key}$";
                if (Kval.typeCode != TypeCode.DBNull && Kval.typeCode != TypeCode.Empty && Kval.typeCode != TypeCode.Object)
                    templateData = templateData.Replace(find, Kval.Value.ToString());
            }
            return templateData;
        }

    }
}
