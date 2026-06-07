using AutoMapper;
using MediatR;
using Lus.Application.ProjectsTimes.Entities;
using Lus.Application.ProjectsTimes.Repositories;
using Lus.Contracts.ProjectsTimes;
using Lus.Application.Common.Exceptions;
using Lus.Application.Common.Extensions;

namespace Lus.Application.ProjectsTimes.Commands.ModifyProjectTime
{
    public class ModifyProjectTimeCommandHandler : IRequestHandler<ModifyProjectTimeCommand, ProjectTimeDto>
    {
        private readonly IProjectsTimesRepository projectsTimesRepository;
        private readonly IMapper mapper;
        private readonly List<string> ProjectTimeListOfPropertiesToIgnore =  new List<string> { "ProjectTemplateId" };

        public ModifyProjectTimeCommandHandler(IProjectsTimesRepository projectsTimesRepository, IMapper mapper)
        {
            this.projectsTimesRepository = projectsTimesRepository;
            this.mapper = mapper;
        }

        public async Task<ProjectTimeDto> Handle(ModifyProjectTimeCommand modifyCommand, CancellationToken cancellationToken)
        {

            if (modifyCommand.Id > 0)
            {
                var savedProjectTime = await this.projectsTimesRepository.GetAsync(prj => prj.Id == modifyCommand.Id, cancellationToken);

                if (savedProjectTime == null)
                {
                    throw new EntityNotFoundException(nameof(savedProjectTime), -1);
                }
                savedProjectTime.CopyIfDifferent(modifyCommand, ProjectTimeListOfPropertiesToIgnore);

                await this.projectsTimesRepository.UpdateAsync(savedProjectTime, cancellationToken);
                return this.mapper.Map<ProjectTimeDto>(savedProjectTime);
            }
            else
            {
                var projectTime = this.mapper.Map<ProjectTime>(modifyCommand);
                projectTime = await this.projectsTimesRepository.AddAsync(projectTime, cancellationToken);
                return this.mapper.Map<ProjectTimeDto>(projectTime);
            }
        }
    }
}
