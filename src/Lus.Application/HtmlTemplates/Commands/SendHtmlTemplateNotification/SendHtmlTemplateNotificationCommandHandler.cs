using AutoMapper;
using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.HtmlTemplates.Entities;
using Lus.Application.HtmlTemplates.Repositories;
using Lus.Application.Notifications.Commands.SendHtmlTemplate;
using Lus.Application.Users.Repositories;
using Lus.Contracts.HtmlTemplates;
using Lus.Contracts.HtmlTemplates.Types;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;

namespace Lus.Application.HtmlTemplates.Commands.SendHtmlTemplateNotification
{
    public class SendHtmlTemplateNotificationCommandHandler : IRequestHandler<SendHtmlTemplateNotificationCommand, Unit>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;
        private readonly IMediator mediator;

        public SendHtmlTemplateNotificationCommandHandler(IMediator mediator, IUsersRepository usersRepository, IMapper mapper)
        {
            this.usersRepository = usersRepository;
            this.mediator = mediator;
            this.mapper = mapper;
        }

        public async Task<Unit> Handle(SendHtmlTemplateNotificationCommand command, CancellationToken cancellationToken)
        {
            var user = await this.usersRepository.GetSingleEntityAsync(command.UserId, cancellationToken);

            var notificationCommand = new SendHtmlTemplateCommand
            {
                User = this.mapper.Map<UserInfoDto>(user),
                MailType = MailType.DeleteFileToUser,
                Subject = "מחיקה של קובץ לא תקין או לא רלוונטי",
                Name = "מייל דרך אתר מוני ג'ובס, הודעת מחיקה של קובץ",
                HtmlType = HtmlType.HtmlTemplate,
                TemplateData = command.TemplateData
            };

            await this.mediator.Send(notificationCommand, cancellationToken);

            return Unit.Value;
        }
    }
}