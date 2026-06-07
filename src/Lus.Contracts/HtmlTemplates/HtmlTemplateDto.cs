using Lus.Contracts.HtmlTemplates.Types;
using Lus.Contracts.Organizations;

namespace Lus.Contracts.HtmlTemplates
{
    public class HtmlTemplateDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? OrganizationId { get; set; }

        public OrganizationDto Organization { get; set; }

        public string TemplateData { get; set; }

        public string? Subject { get; set; }

        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public bool? Active { get; set; }

        public string? ReplayEmail { get; set; }

        public HtmlType HtmlType { get; set; }
    }
}
