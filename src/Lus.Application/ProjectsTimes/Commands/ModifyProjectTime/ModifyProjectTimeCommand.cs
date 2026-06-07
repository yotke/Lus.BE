using MediatR;
using Lus.Contracts.ProjectsTimes;


namespace Lus.Application.ProjectsTimes.Commands.ModifyProjectTime
{
    public record ModifyProjectTimeCommand : IRequest<ProjectTimeDto>
    {
        public int Id { get; set; }
        public DateTime WorkDate { get; set; }
        public string? TimeData { get; set; }
        public string WorkDescription { get; set; }
        public int? ProjectTemplateId { get; set; }
    }
}
