using System.Text.Json;

namespace Lus.Contracts.Documents.Builder
{
    public sealed class DraftPatchOp
    {
        public required string Op { get; init; }

        public required string Path { get; init; }

        public JsonElement? Value { get; init; }
    }
}
