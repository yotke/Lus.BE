using MediatR;
using Lus.Contracts.HtmlTemplates;
using Lus.Contracts.HtmlTemplates.Types;

namespace Lus.Application.HtmlTemplates.Commands.CreateHtmlTemplate
{
    public record CreateHtmlTemplateCommand : IRequest<HtmlTemplateDto>
    {
        public string Name { get; set; }

        public int? OrganizationId { get; set; }

        public string TemplateData { get; set; }

        public string Subject { get; set; }

        public string? ReplayEmail { get; set; }
      
        public bool? Active { get; set; }

        public HtmlType HtmlType { get; set; }
    }
}
