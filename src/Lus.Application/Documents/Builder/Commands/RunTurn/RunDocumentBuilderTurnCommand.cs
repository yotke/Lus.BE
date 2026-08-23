using MediatR;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Commands.RunTurn
{
    public sealed record RunDocumentBuilderTurnCommand : IRequest<DocumentBuilderTurnResponseDto>
    {
        public int Version { get; init; }
        public string? Text { get; init; }
        /// <summary>The question this message answers, when it answers one.</summary>
        public string? QuestionId { get; init; }
    }
}
