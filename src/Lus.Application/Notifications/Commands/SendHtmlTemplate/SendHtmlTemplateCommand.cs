using MediatR;
using Lus.Contracts.HtmlTemplates.Types;
using Lus.Contracts.Notifications.Types;
using Lus.Contracts.Users;

namespace Lus.Application.Notifications.Commands.SendHtmlTemplate
{
    public record SendHtmlTemplateCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        public UserInfoDto User { get; set; }

        public string Name { get; set; }

        public int? OrganizationId { get; set; }

        public int ApplicationDateId { get; set; }

        public string TemplateData { get; set; }

        public string? Subject { get; set; }

        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public bool? Active { get; set; }

        public string? ReplayEmail { get; set; }

        public HtmlType HtmlType { get; set; }

        public MailType MailType { get; set; }
    }
}
