using MediatR;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Commands.CanvasEdit
{
    public sealed record ApplyCanvasEditCommand : IRequest<DocumentBuilderTurnResponseDto>
    {
        public int Version { get; init; }
        public IReadOnlyList<DraftPatchOp> Ops { get; init; } = Array.Empty<DraftPatchOp>();
    }
}
