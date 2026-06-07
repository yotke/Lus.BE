using Lus.Application.Common;
using Lus.Application.Organizations.Entities;
using Lus.Contracts.HtmlTemplates.Types;

namespace Lus.Application.HtmlTemplates.Entities
{
    public class HtmlTemplate : EntityBase<int>
    {
        public string Name { get; set; }

        public int? OrganizationId { get; set; }

        public Organization? Organization { get; set; }

        public string TemplateData { get; set; }

        public string Subject { get; set; }

        public string? ReplayEmail { get; set; }

        public HtmlType HtmlType { get; set; }


    }
}
