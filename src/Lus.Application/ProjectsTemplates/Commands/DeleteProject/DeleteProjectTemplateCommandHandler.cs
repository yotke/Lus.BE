using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.ProjectsTemplates.Repositories;

namespace Lus.Application.ProjectsTemplates.Commands.DeleteProject
{
    public class DeleteProjectTemplateCommandHandler : IRequestHandler<DeleteProjectTemplateCommand, Unit>
    {
        private readonly IProjectsTemplatesRepository ProjectsTemplatesRepository;

        public DeleteProjectTemplateCommandHandler(IProjectsTemplatesRepository ProjectsTemplatesRepository) => this.ProjectsTemplatesRepository = ProjectsTemplatesRepository;

        public async Task<Unit> Handle(DeleteProjectTemplateCommand request, CancellationToken cancellationToken)
        {
            var projectTemplate = await this.ProjectsTemplatesRepository.GetSingleEntityAsync(request.Id, cancellationToken);
            await this.ProjectsTemplatesRepository.DeleteAsync(projectTemplate, cancellationToken);

            return Unit.Value;
        }
    }
}
