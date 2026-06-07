using MediatR;
using Lus.Contracts.ProjectsTemplates;

namespace Lus.Application.ProjectsTemplates.Commands.ModifyProject
{
    public record ModifyProjectTemplateCommand : IRequest<ProjectTemplateDto>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ProjectNumber { get; set; }
        public string SectionName { get; set; }
        public DateTime CurrentDate { get; set; }
        public string AccountNumber { get; set; }
        public string ProjectSubject { get; set; }
        public string WorkKindRate { get; set; }
        public string WorkRate { get; set; }
        public string WorkerName { get; set; }
        public string ProjectLocation { get; set; }
        public string ConstrctorName { get; set; }
        public DateTime StartContractDate { get; set; }
        public DateTime EndContractDate { get; set; }
        public string WorkContractNumber { get; set; }
        public string EmployeeSectionName { get; set; }
        public string ConstrctorPhone { get; set; }
        public string ConstrctorTitle { get; set; }
        public string ConstrctorAddress { get; set; }
        public string ProjectManager { get; set; }
        public string ConstrctorEntrepreneurNumber { get; set; }
        public int? OrganizationId { get; set; }

    }
}
