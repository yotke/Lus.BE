using Lus.Contracts.Notifications.Types;

namespace Lus.Contracts.HtmlTemplates
{
    public class ModifyHtmlTemplateDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? OrganizationId { get; set; }

        public string TemplateData { get; set; }
        
        public bool Active { get; set; }

        public string? Subject { get; set; }

        public string? ReplayEmail { get; set; }

        public int HtmlType { get; set; }
    }
}
