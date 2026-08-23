using MediatR;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Commands.Undo
{
    public sealed record UndoDocumentBuilderCommand : IRequest<DocumentBuilderTurnResponseDto>;
}
