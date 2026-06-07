using MediatR;
using Lus.Contracts.ProjectsTemplates;

namespace Lus.Application.ProjectsTemplates.Queries.GetProjectsByOrganizationId
{
    public record GetProjectsTemplatesQueryByOrgId(int organizationId) : IRequest<ICollection<ProjectTemplateDto>>;
}
