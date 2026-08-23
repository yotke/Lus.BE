using MediatR;
using Lus.Application.Documents.Builder.Orchestration;
using Lus.Authorization;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Commands.RunTurn
{
    public sealed class RunDocumentBuilderTurnCommandHandler
        : IRequestHandler<RunDocumentBuilderTurnCommand, DocumentBuilderTurnResponseDto>
    {
        private readonly DocumentBuilderOrchestrator orchestrator;
        private readonly IUserAccessor users;

        public RunDocumentBuilderTurnCommandHandler(
            DocumentBuilderOrchestrator orchestrator,
            IUserAccessor users)
        {
            this.orchestrator = orchestrator;
            this.users = users;
        }

        public async Task<DocumentBuilderTurnResponseDto> Handle(
            RunDocumentBuilderTurnCommand request, CancellationToken cancellationToken)
        {
            var result = await this.orchestrator.RunTurnAsync(
                this.users.ProjectUser.Id, request.Version, request.Text, request.QuestionId, cancellationToken);
            return new DocumentBuilderTurnResponseDto
            {
                Version = result.Version,
                Ops = result.Ops.ToList(),
                Question = result.Question,
                Messages = result.Messages.ToList(),
                Warnings = result.Warnings.ToList(),
            };
        }
    }
}
