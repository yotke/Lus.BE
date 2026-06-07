using AutoMapper;
using Lus.Application.Common.Exceptions;
using Lus.Application.Common.Extensions;
using Lus.Application.ProjectsTemplates.Entities;
using Lus.Application.ProjectsTemplates.Repositories;
using Lus.Contracts.ProjectsTemplates;
using MediatR;


namespace Lus.Application.ProjectsTemplates.Commands.ModifyProject
{
    public class ModifyProjectTemplateCommandHandler : IRequestHandler<ModifyProjectTemplateCommand, ProjectTemplateDto>
    {
        private readonly IProjectsTemplatesRepository ProjectsTemplatesRepository;
        private readonly IMapper mapper;
        private readonly List<string> ProjectTemplateListOfPropertiesToIgnore =
     new List<string> { "OrganizationId", "Organization" };
        public ModifyProjectTemplateCommandHandler(IProjectsTemplatesRepository ProjectsTemplatesRepository, IMapper mapper)
        {
            this.ProjectsTemplatesRepository = ProjectsTemplatesRepository;
            this.mapper = mapper;
        }

        public async Task<ProjectTemplateDto> Handle(ModifyProjectTemplateCommand modifyCommand, CancellationToken cancellationToken)
        {
            var savedProject = await this.ProjectsTemplatesRepository.GetAsync(prj => prj.Id == modifyCommand.Id, cancellationToken);

            if (savedProject == null)
            {
                throw new EntityNotFoundException(nameof(savedProject), -1);
            }
            savedProject.CopyIfDifferent(modifyCommand, ProjectTemplateListOfPropertiesToIgnore);

            var projectTemplate = await this.ProjectsTemplatesRepository.UpdateAsync(savedProject, cancellationToken);

            return this.mapper.Map<ProjectTemplateDto>(projectTemplate);
        }
    }
}
