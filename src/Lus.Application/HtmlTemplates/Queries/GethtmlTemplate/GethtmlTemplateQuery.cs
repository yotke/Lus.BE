using MediatR;
using Lus.Contracts.HtmlTemplates;

namespace Lus.Application.HtmlTemplates.Queries.GetHtmlTemplate
{
    public record GetHtmlTemplateQuery(int HtmlTemplateId) : IRequest<HtmlTemplateDto>;
}
