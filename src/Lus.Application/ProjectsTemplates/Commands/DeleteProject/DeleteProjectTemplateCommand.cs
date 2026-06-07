using MediatR;

namespace Lus.Application.ProjectsTemplates.Commands.DeleteProject
{
    public record DeleteProjectTemplateCommand(int Id) : IRequest<Unit>;
}
