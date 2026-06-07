using AutoMapper;
using MediatR;
using Lus.Application.ProjectsTimes.Entities;
using Lus.Application.ProjectsTimes.Repositories;
using Lus.Contracts.ProjectsTimes;

namespace Lus.Application.ProjectsTimes.Commands.CreateProjectTime
{
    public class CreateProjectTimeCommandHandler : IRequestHandler<CreateProjectTimeCommand, ProjectTimeDto>
    {
        private readonly IProjectsTimesRepository projectsTimesRepository;
        private readonly IMapper mapper;

        public CreateProjectTimeCommandHandler(IProjectsTimesRepository projectsTimesRepository, IMapper mapper)
        {
            this.projectsTimesRepository = projectsTimesRepository;
            this.mapper = mapper;
        }

        public async Task<ProjectTimeDto> Handle(CreateProjectTimeCommand createCommand, CancellationToken cancellationToken)
        {
            var projectTime = this.mapper.Map<ProjectTime>(createCommand);

            projectTime = await this.projectsTimesRepository.AddAsync(projectTime, cancellationToken);

            return this.mapper.Map<ProjectTimeDto>(projectTime);
        }
    }
}
