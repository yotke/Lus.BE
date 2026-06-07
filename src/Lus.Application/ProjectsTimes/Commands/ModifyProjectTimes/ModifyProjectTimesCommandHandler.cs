using AutoMapper;
using MediatR;
using Lus.Application.ProjectsTimes.Entities;
using Lus.Application.ProjectsTimes.Repositories;
using Lus.Contracts.ProjectsTimes;
using Lus.Application.Common.Extensions;
using Lus.Application.ProjectsTimes.Commands.ModifyProjectTimes;

namespace Lus.Application.ProjectsTimes.Commands.ModifyProjectsTimes
{
    public class ModifyProjectsTimesCommandHandler : IRequestHandler<ModifyProjectTimesCommand, ICollection<ProjectTimeDto>>
    {
        private readonly IProjectsTimesRepository ProjectsTimesRepository;
        private readonly IMapper mapper;
        private readonly List<string> projectTimeListOfPropertiesToIgnore = new List<string> { "ProjectTemplateId" };
        public ModifyProjectsTimesCommandHandler(IProjectsTimesRepository ProjectsTimesRepository, IMapper mapper)
        {
            this.ProjectsTimesRepository = ProjectsTimesRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<ProjectTimeDto>> Handle(ModifyProjectTimesCommand modifyCommand, CancellationToken cancellationToken)
        {
            var projectTimes = new List<ProjectTimeDto>();
            foreach (var projectTime in modifyCommand.ProjectTimes)
            {
                if (projectTime.Id > 0)
                {
                    var savedProjectTime =
                        await this.ProjectsTimesRepository.GetSingleEntityAsync(projectTime.Id, cancellationToken: cancellationToken);
                    savedProjectTime.CopyIfDifferent(projectTime, projectTimeListOfPropertiesToIgnore);
                    savedProjectTime.TimeData = projectTime.JsonTime;
                    projectTimes.Add(
                        this.mapper.Map<ProjectTimeDto>(
                            await this.ProjectsTimesRepository.UpdateAsync(savedProjectTime, cancellationToken)));
                }
                else
                {
                    projectTimes.Add(this.mapper.Map<ProjectTimeDto>(
                        await this.ProjectsTimesRepository.AddAsync(this.mapper.Map<ProjectTime>(projectTime), cancellationToken)));
                }
            }

            return projectTimes;
        }
    }
}
