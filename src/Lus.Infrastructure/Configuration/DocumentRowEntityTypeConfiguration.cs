using Lus.Application.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lus.Infrastructure.Configuration
{
    public sealed class DocumentRowEntityTypeConfiguration : IEntityTypeConfiguration<DocumentRow>
    {
        public void Configure(EntityTypeBuilder<DocumentRow> builder)
        {
            AuditableEntityConfiguration.ConfigureAudit(builder);
            builder.ToTable("DocumentRows");
            builder.Property(r => r.Hours).HasColumnType("decimal(12,2)");
            builder.Property(r => r.Location).HasMaxLength(200);
            builder.Property(r => r.Subject).HasMaxLength(2000);
        }
    }
}
