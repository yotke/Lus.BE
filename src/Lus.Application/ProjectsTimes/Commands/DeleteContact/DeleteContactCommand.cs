using MediatR;

namespace Lus.Application.ProjectsTimes.Commands.DeleteProjectTime
{
    public record DeleteProjectTimeCommand(int Id) : IRequest<Unit>;
}
