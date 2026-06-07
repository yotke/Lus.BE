using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Lus.Application.Contacts.Entities;
using Lus.Application.Contacts.Repositories;
using Lus.Application.Notifications.Commands.SendHtmlTemplate;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;

namespace Lus.Application.HtmlTemplates.Commands.SendContactUsNotification
{
    public class SendContactUsNotificationCommandHandler : IRequestHandler<SendContactUsNotificationCommand, Unit>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IContactsRepository contactsRepository;
        private readonly IMapper mapper;
        private readonly IMediator mediator;

        public SendContactUsNotificationCommandHandler(IMediator mediator, IUsersRepository usersRepository, IMapper mapper, IContactsRepository contactsRepository)
        {
            this.usersRepository = usersRepository;
            this.contactsRepository = contactsRepository;
            this.mediator = mediator;
            this.mapper = mapper;
        }

        public async Task<Unit> Handle(SendContactUsNotificationCommand command, CancellationToken cancellationToken)
        {
            var userToSend = await this.usersRepository.GetWithIncludeAsync(u => u.Email == command.ReplayEmail, cancellationToken, u => u.UserOrganizations);

            IEnumerable<Contact> contacts = new List<Contact>();
            if (userToSend?.UserOrganizations?.Any() ?? false)
            {
                var organizationId = userToSend.UserOrganizations.First().OrganizationId;
                contacts = await this.contactsRepository.GetAllListAsync(c => c.OrganizationId.HasValue && c.OrganizationId == organizationId, cancellationToken);
            }
            else
            {
                contacts = await this.contactsRepository.GetAllListAsync(c => !c.OrganizationId.HasValue, cancellationToken);
            }

            foreach (var contact in contacts)
            {
                var notificationCommand = new SendHtmlTemplateCommand
                {
                    User = this.mapper.Map<UserInfoDto>(contact),
                    MailType = MailType.ContactUsMail,
                    TemplateData = command.TemplateData,
                    Subject = "צור/י איתנו קשר"
                };

                await this.mediator.Send(notificationCommand, cancellationToken);
            }


            return Unit.Value;
        }
    }
}
