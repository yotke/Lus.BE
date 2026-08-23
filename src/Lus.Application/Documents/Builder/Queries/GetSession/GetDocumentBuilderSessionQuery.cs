using MediatR;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Queries.GetSession
{
    public sealed record GetDocumentBuilderSessionQuery : IRequest<DocumentBuilderSessionDto>;
}
