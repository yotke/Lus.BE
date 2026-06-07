using MediatR;
using Lus.Contracts.ProjectsTimes;
using Lus.Application.ProjectsTimes.Entities;

namespace Lus.Application.ProjectsTimes.Commands.ModifyProjectTimes
{
    public record ModifyProjectTimesCommand(ICollection<ModifyProjectTimeDto> ProjectTimes) : IRequest<ICollection<ProjectTimeDto>>;

}
