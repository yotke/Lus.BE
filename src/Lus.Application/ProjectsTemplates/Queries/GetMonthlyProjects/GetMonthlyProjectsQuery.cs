using MediatR;
using Lus.Contracts.ProjectsTemplates;

namespace Lus.Application.ProjectsTemplates.Queries.GetMonthlyProjects
{
    public record GetMonthlyProjectsQuery(DateTime monthDate) : IRequest<ICollection<ProjectTemplateDto>>;
}
