using AutoMapper;
using MediatR;
using Lus.Application.ProjectsTimes.Entities;
using Lus.Application.ProjectsTimes.Repositories;
using Lus.Contracts.ProjectsTimes;
using System.Collections.Generic;

namespace Lus.Application.ProjectsTimes.Commands.CreateProjectsTime
{
    public class CreateProjectTimesCommandHandler : IRequestHandler<CreateProjectTimesCommand, ICollection<ProjectTimeDto>>
    {
        private readonly IProjectsTimesRepository projectsTimesRepository;
        private readonly IMapper mapper;

        public CreateProjectTimesCommandHandler(IProjectsTimesRepository projectsTimesRepository, IMapper mapper)
        {
            this.projectsTimesRepository = projectsTimesRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<ProjectTimeDto>> Handle(CreateProjectTimesCommand createCommand, CancellationToken cancellationToken)
        {
            var projectTimes = this.mapper.Map<ICollection<ProjectTime>>(createCommand.ProjectTimes);
            await this.projectsTimesRepository.AddAllAsync(projectTimes, cancellationToken);
            return this.mapper.Map<ICollection<ProjectTimeDto>>(projectTimes);
        }
    }
}
