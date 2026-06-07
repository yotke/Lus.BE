using AutoMapper;
using MediatR;
using Lus.Application.ProjectsTimes.Repositories;
using Lus.Contracts.ProjectsTimes;

namespace Lus.Application.ProjectsTimes.Queries.GetProjectTimesByProjectId
{
    public class GetProjectTimesByProjectIdQueryHandler : IRequestHandler<GetProjectTimesByProjectIdQuery, ICollection<ProjectTimeDto>>
    {
        private readonly IProjectsTimesRepository projectsTimesRepository;
        private readonly IMapper mapper;

        public GetProjectTimesByProjectIdQueryHandler(IProjectsTimesRepository projectsTimesRepository, IMapper mapper)
        {
            this.projectsTimesRepository = projectsTimesRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<ProjectTimeDto>> Handle(GetProjectTimesByProjectIdQuery request,
            CancellationToken cancellationToken)
        {
            var projectsTimes = await projectsTimesRepository.GetAllListAsync(c => c.ProjectTemplateId == request.projectId, cancellationToken);

            var projectsTimesDto = mapper.Map<ICollection<ProjectTimeDto>>(projectsTimes);

            return projectsTimesDto;
        }
    }
}
