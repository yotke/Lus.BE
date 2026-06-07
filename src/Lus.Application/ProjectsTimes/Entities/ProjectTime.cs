using Lus.Application.Common;
using Lus.Application.Organizations.Entities;
using Lus.Application.ProjectsTemplates.Entities;
using Microsoft.AspNetCore.Http;

namespace Lus.Application.ProjectsTimes.Entities
{
    public class ProjectTime : EntityBase<int>
    {
        public DateTime WorkDate { get; set; }
        public string? TimeData { get; set; }
        public string WorkDescription { get; set; }
        public int ProjectTemplateId { get; set; }
        public ProjectTemplate? ProjectTemplate { get; set; }
    }
}
