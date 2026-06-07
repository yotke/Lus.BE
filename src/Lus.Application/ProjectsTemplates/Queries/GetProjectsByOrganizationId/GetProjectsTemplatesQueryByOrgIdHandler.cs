using AutoMapper;
using MediatR;
using Lus.Application.ProjectsTemplates.Repositories;
using Lus.Contracts.ProjectsTemplates;

namespace Lus.Application.ProjectsTemplates.Queries.GetProjectsByOrganizationId
{
    public class GetProjectsTemplatesQueryByOrgIdHandler : IRequestHandler<GetProjectsTemplatesQueryByOrgId, ICollection<ProjectTemplateDto>>
    {
        private readonly IProjectsTemplatesRepository projectTemplatesRepository;
        private readonly IMapper mapper;

        public GetProjectsTemplatesQueryByOrgIdHandler(IProjectsTemplatesRepository projectTemplatesRepository, IMapper mapper)
        {
            this.projectTemplatesRepository = projectTemplatesRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<ProjectTemplateDto>> Handle(GetProjectsTemplatesQueryByOrgId request,
            CancellationToken cancellationToken)
        {
            var projectsTemplates = await projectTemplatesRepository.GetAllListAsync(c => c.OrganizationId == request.organizationId, cancellationToken);

            var projectsTemplatesDto = mapper.Map<ICollection<ProjectTemplateDto>>(projectsTemplates);

            return projectsTemplatesDto;
        }
    }
}
