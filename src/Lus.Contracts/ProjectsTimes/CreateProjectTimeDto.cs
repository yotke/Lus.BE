using Lus.Contracts.ProjectsTemplates;
using Lus.Contracts.ProjectsTimes.Models;

namespace Lus.Contracts.ProjectsTimes
{
    public class CreateProjectTimeDto
    {
        public string? ProjectName { get; set; }
        public int? ProjectNumber { get; set; }
        public string? sectionName { get; set; }
        public DateTime WorkDate { get; set; }
        public int? ProjectTemplateId { get; set; }
        public string WorkDescription { get; set; }
        public ProjectTemplateDto? ProjectTemplate { get; set; }
        public TimesArrayDto? TimesArray { get; set; }
        public string? JsonTime { get; set; }
    }
}
