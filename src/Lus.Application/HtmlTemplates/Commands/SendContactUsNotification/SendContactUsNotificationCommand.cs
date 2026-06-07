using MediatR;

namespace Lus.Application.HtmlTemplates.Commands.SendContactUsNotification
{
    public record SendContactUsNotificationCommand : IRequest<Unit>
    {
        public string Name { get; set; }

        public string ReplayEmail { get; set; }

        public string UpdatedById { get; set; }

        public string TemplateData { get; set; }
    }
}
