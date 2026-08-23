using Lus.Application.Common;

namespace Lus.Application.Documents.Entities
{
    /// <summary>Durable rescue row for the Redis builder session. SchemaVersion is discard-not-migrate.</summary>
    public class DocumentBuildSessionRow : EntityBase<int>
    {
        public int UserId { get; set; }

        public int? InstanceId { get; set; }

        public int SchemaVersion { get; set; }

        public int Version { get; set; }

        public string DraftJson { get; set; } = "{}";
    }
}
