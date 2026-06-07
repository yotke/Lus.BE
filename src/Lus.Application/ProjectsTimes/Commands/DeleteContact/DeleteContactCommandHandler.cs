using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.ProjectsTimes.Repositories;

namespace Lus.Application.ProjectsTimes.Commands.DeleteProjectTime
{
    public class DeleteProjectTimeCommandHandler : IRequestHandler<DeleteProjectTimeCommand, Unit>
    {
        private readonly IProjectsTimesRepository projectTimesRepository;

        public DeleteProjectTimeCommandHandler(IProjectsTimesRepository projectTimesRepository) => this.projectTimesRepository = projectTimesRepository;

        public async Task<Unit> Handle(DeleteProjectTimeCommand request, CancellationToken cancellationToken)
        {
            var projectTime = await this.projectTimesRepository.GetSingleEntityAsync(request.Id, cancellationToken);
            await this.projectTimesRepository.DeleteAsync(projectTime, cancellationToken);

            return Unit.Value;
        }
    }
}
