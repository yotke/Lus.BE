using MediatR;
using Lus.Application.Documents.Builder.Orchestration;
using Lus.Authorization;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Commands.CanvasEdit
{
    public sealed class ApplyCanvasEditCommandHandler
        : IRequestHandler<ApplyCanvasEditCommand, DocumentBuilderTurnResponseDto>
    {
        private readonly DocumentBuilderOrchestrator orchestrator;
        private readonly IUserAccessor users;

        public ApplyCanvasEditCommandHandler(
            DocumentBuilderOrchestrator orchestrator,
            IUserAccessor users)
        {
            this.orchestrator = orchestrator;
            this.users = users;
        }

        public async Task<DocumentBuilderTurnResponseDto> Handle(
            ApplyCanvasEditCommand request, CancellationToken cancellationToken)
        {
            var result = await this.orchestrator.ApplyCanvasEditAsync(
                this.users.ProjectUser.Id, request.Version, request.Ops, cancellationToken);
            return new DocumentBuilderTurnResponseDto
            {
                Version = result.Version,
                Ops = result.Ops.ToList(),
                // A canvas edit runs the same interview a turn does, so it answers with the
                // same shape: the next question, any advice, and the validator's findings.
                Question = result.Question,
                Messages = result.Messages.ToList(),
                Warnings = result.Warnings.ToList(),
            };
        }
    }
}
