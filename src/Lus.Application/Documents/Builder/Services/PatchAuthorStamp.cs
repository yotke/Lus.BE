using System.Text.Json;
using System.Text.Json.Nodes;
using Lus.Contracts.Documents.Builder;

namespace Lus.Application.Documents.Builder.Services
{
    /// <summary>
    /// Records who wrote each row.
    ///
    /// A user reviewing their report has to be able to separate their own corrections from
    /// what an agent filled in — otherwise "check it before you sign it" is not a thing they
    /// can actually do. Stamped at collection time, so the row carries the specific agent that
    /// produced it rather than a generic "AI".
    /// </summary>
    public static class PatchAuthorStamp
    {
        /// <summary>Author value for a hand edit made on the canvas.</summary>
        public const string User = "user";

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static List<DraftPatchOp> Stamp(
            IReadOnlyList<DraftPatchOp> ops, string author, DateTime? nowUtc = null)
        {
            var stampedAt = nowUtc ?? DateTime.UtcNow;
            var result = new List<DraftPatchOp>(ops.Count);

            foreach (var op in ops)
            {
                // Only row writes carry provenance; totals and template fields are document
                // properties, not authored content.
                if (op.Op is not ("AddRow" or "UpdateRow") || op.Value is null)
                {
                    result.Add(op);
                    continue;
                }

                var node = JsonNode.Parse(op.Value.Value.GetRawText());
                if (node is not JsonObject obj)
                {
                    result.Add(op);
                    continue;
                }

                obj["Source"] = author;
                obj["ChangedAt"] = stampedAt;

                result.Add(new DraftPatchOp
                {
                    Op = op.Op,
                    Path = op.Path,
                    Value = JsonSerializer.SerializeToElement(obj, Json),
                });
            }

            return result;
        }
    }
}
