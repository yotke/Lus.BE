using MediatR;
using Lus.Application.Documents.Builder.Orchestration;
using Lus.Authorization;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Queries.GetSession
{
    public sealed class GetDocumentBuilderSessionQueryHandler
        : IRequestHandler<GetDocumentBuilderSessionQuery, DocumentBuilderSessionDto>
    {
        private readonly DocumentBuilderOrchestrator orchestrator;
        private readonly IUserAccessor users;

        public GetDocumentBuilderSessionQueryHandler(
            DocumentBuilderOrchestrator orchestrator,
            IUserAccessor users)
        {
            this.orchestrator = orchestrator;
            this.users = users;
        }

        public async Task<DocumentBuilderSessionDto> Handle(
            GetDocumentBuilderSessionQuery request, CancellationToken cancellationToken)
        {
            var result = await this.orchestrator.GetSessionAsync(this.users.ProjectUser.Id, cancellationToken);
            return new DocumentBuilderSessionDto { Version = result.Version, Draft = result.Draft };
        }
    }
}
