using MediatR;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Commands.Redo
{
    public sealed record RedoDocumentBuilderCommand : IRequest<DocumentBuilderTurnResponseDto>;
}
