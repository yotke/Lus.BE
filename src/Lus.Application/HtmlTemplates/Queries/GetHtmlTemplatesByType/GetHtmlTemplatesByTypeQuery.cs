using MediatR;
using Lus.Contracts.HtmlTemplates;
using Lus.Contracts.HtmlTemplates.Types;

namespace Lus.Application.HtmlTemplates.Queries.GetHtmlTemplatesByType
{
    public record GetHtmlTemplatesByTypeQuery(HtmlType HtmlType) : IRequest<ICollection<HtmlTemplateDto>>;
}
