using Lus.Application.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lus.Infrastructure.Configuration
{
    public sealed class DocumentSeriesEntityTypeConfiguration : IEntityTypeConfiguration<DocumentSeries>
    {
        public void Configure(EntityTypeBuilder<DocumentSeries> builder)
        {
            AuditableEntityConfiguration.ConfigureAudit(builder);
            builder.ToTable("DocumentSeries");
            builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
            builder.Property(s => s.ClientName).IsRequired().HasMaxLength(200);
            builder.Property(s => s.OrganizationId).IsRequired(false);
            builder.Property(s => s.ExemplarFileId).IsRequired(false);
            builder.Property(s => s.SourceFormat).IsRequired();

            builder.HasMany(s => s.Instances)
                .WithOne(i => i.Series)
                .HasForeignKey(i => i.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.RateCards)
                .WithOne(r => r.Series)
                .HasForeignKey(r => r.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
