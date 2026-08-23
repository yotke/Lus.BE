using MediatR;
using Lus.Application.Documents.Builder.Orchestration;
using Lus.Authorization;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Commands.Undo
{
    public sealed class UndoDocumentBuilderCommandHandler
        : IRequestHandler<UndoDocumentBuilderCommand, DocumentBuilderTurnResponseDto>
    {
        private readonly DocumentBuilderOrchestrator orchestrator;
        private readonly IUserAccessor users;

        public UndoDocumentBuilderCommandHandler(
            DocumentBuilderOrchestrator orchestrator,
            IUserAccessor users)
        {
            this.orchestrator = orchestrator;
            this.users = users;
        }

        public async Task<DocumentBuilderTurnResponseDto> Handle(
            UndoDocumentBuilderCommand request, CancellationToken cancellationToken)
        {
            var result = await this.orchestrator.UndoAsync(this.users.ProjectUser.Id, cancellationToken);
            return new DocumentBuilderTurnResponseDto { Version = result.Version, Ops = result.Ops.ToList() };
        }
    }
}
