using MediatR;
using Lus.Contracts.ProjectsTimes;


namespace Lus.Application.ProjectsTimes.Commands.CreateProjectTime
{
    public record CreateProjectTimeCommand : IRequest<ProjectTimeDto>
    {
        public DateTime WorkDate { get; set; }
        public string TimeData { get; set; }
        public string WorkDescription { get; set; }
        public int? ProjectTemplateId { get; set; }
    }
}
