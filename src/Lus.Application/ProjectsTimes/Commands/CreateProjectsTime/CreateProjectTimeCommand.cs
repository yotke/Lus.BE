using MediatR;
using Lus.Contracts.ProjectsTimes;


namespace Lus.Application.ProjectsTimes.Commands.CreateProjectsTime
{
    public record CreateProjectTimesCommand(ICollection<CreateProjectTimeDto> ProjectTimes) : IRequest<ICollection<ProjectTimeDto>>;
   
}
