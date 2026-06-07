using MediatR;

namespace Lus.Application.HtmlTemplates.Commands.SendHtmlTemplateNotification
{
    public record SendHtmlTemplateNotificationCommand(int UserId, string TemplateData) : IRequest<Unit>;
}
