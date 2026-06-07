using MediatR;
using Lus.Contracts.ProjectsTimes;

namespace Lus.Application.ProjectsTimes.Queries.GetProjectTimesByProjectId
{
    public record GetProjectTimesByProjectIdQuery(int projectId) : IRequest<ICollection<ProjectTimeDto>>;
}
