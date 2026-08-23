using Lus.Application.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lus.Infrastructure.Configuration
{
    public sealed class DocumentBuildSessionRowEntityTypeConfiguration : IEntityTypeConfiguration<DocumentBuildSessionRow>
    {
        public void Configure(EntityTypeBuilder<DocumentBuildSessionRow> builder)
        {
            AuditableEntityConfiguration.ConfigureAudit(builder);
            builder.ToTable("DocumentBuildSessions");
            builder.HasIndex(s => s.UserId);
            builder.Property(s => s.DraftJson).IsRequired().HasColumnType("LONGTEXT");
            builder.Property(s => s.SchemaVersion).IsRequired();
            builder.Property(s => s.Version).IsRequired();
        }
    }
}
