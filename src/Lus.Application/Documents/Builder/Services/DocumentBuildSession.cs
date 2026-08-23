using Lus.Application.Common.Builders;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Services
{
    public sealed class DocumentBuildSession : IBuilderSession
    {
        public const int CurrentSchemaVersion = 1;

        public int UserId { get; set; }
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public DocumentDraftDto Draft { get; set; } = new();
        public List<List<DraftPatchOp>> UndoInverses { get; set; } = new();
        public List<List<DraftPatchOp>> UndoForwards { get; set; } = new();
        public List<List<DraftPatchOp>> RedoForwards { get; set; } = new();
        public List<List<DraftPatchOp>> RedoInverses { get; set; } = new();
    }
}
