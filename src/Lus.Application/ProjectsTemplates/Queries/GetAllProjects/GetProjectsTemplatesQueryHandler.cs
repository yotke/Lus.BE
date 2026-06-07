using AutoMapper;
using MediatR;
using Lus.Application.ProjectsTemplates.Repositories;
using Lus.Contracts.ProjectsTemplates;

namespace Lus.Application.ProjectsTemplates.Queries.GetAllProjects
{
    public class GetProjectsTemplatesQueryHandler : IRequestHandler<GetProjectsTemplatesQuery, ICollection<ProjectTemplateDto>>
    {
        private readonly IProjectsTemplatesRepository projectsTemplatesRepository;
        private readonly IMapper mapper;

        public GetProjectsTemplatesQueryHandler(IProjectsTemplatesRepository projectsTemplatesRepository, IMapper mapper)
        {
            this.projectsTemplatesRepository = projectsTemplatesRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<ProjectTemplateDto>> Handle(GetProjectsTemplatesQuery request,
            CancellationToken cancellationToken)
        {
            var ProjectsTemplates = await projectsTemplatesRepository.GetAllListAsync(p => true,cancellationToken, s => s.ProjectTimes);

            var ProjectsTemplatesDto = mapper.Map<ICollection<ProjectTemplateDto>>(ProjectsTemplates);

            return ProjectsTemplatesDto;
        }
    }
}
