using MediatR;
using Lus.Contracts.ProjectsTemplates;

namespace Lus.Application.ProjectsTemplates.Queries.GetAllProjects
{
    public record GetProjectsTemplatesQuery() : IRequest<ICollection<ProjectTemplateDto>>;
}
