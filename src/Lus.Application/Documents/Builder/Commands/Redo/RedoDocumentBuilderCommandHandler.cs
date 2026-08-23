using MediatR;
using Lus.Application.Documents.Builder.Orchestration;
using Lus.Authorization;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Commands.Redo
{
    public sealed class RedoDocumentBuilderCommandHandler
        : IRequestHandler<RedoDocumentBuilderCommand, DocumentBuilderTurnResponseDto>
    {
        private readonly DocumentBuilderOrchestrator orchestrator;
        private readonly IUserAccessor users;

        public RedoDocumentBuilderCommandHandler(
            DocumentBuilderOrchestrator orchestrator,
            IUserAccessor users)
        {
            this.orchestrator = orchestrator;
            this.users = users;
        }

        public async Task<DocumentBuilderTurnResponseDto> Handle(
            RedoDocumentBuilderCommand request, CancellationToken cancellationToken)
        {
            var result = await this.orchestrator.RedoAsync(this.users.ProjectUser.Id, cancellationToken);
            return new DocumentBuilderTurnResponseDto { Version = result.Version, Ops = result.Ops.ToList() };
        }
    }
}
