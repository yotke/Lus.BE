using AutoMapper;
using Lus.Application.ProjectsTemplates.Entities;
using Lus.Application.ProjectsTemplates.Repositories;
using Lus.Contracts.ProjectsTemplates;
using MediatR;


namespace Lus.Application.ProjectsTemplates.Commands.CreateProject
{
    public class CreateProjectTemplateCommandHandler : IRequestHandler<CreateProjectTemplateCommand, ProjectTemplateDto>
    {
        private readonly IProjectsTemplatesRepository ProjectsTemplatesRepository;
        private readonly IMapper mapper;

        public CreateProjectTemplateCommandHandler(IProjectsTemplatesRepository ProjectsTemplatesRepository, IMapper mapper)
        {
            this.ProjectsTemplatesRepository = ProjectsTemplatesRepository;
            this.mapper = mapper;
        }

        public async Task<ProjectTemplateDto> Handle(CreateProjectTemplateCommand createCommand, CancellationToken cancellationToken)
        {
            var projectTemplate = this.mapper.Map<ProjectTemplate>(createCommand);

            projectTemplate = await this.ProjectsTemplatesRepository.AddAsync(projectTemplate, cancellationToken);

            return this.mapper.Map<ProjectTemplateDto>(projectTemplate);
        }
    }
}
