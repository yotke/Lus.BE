using Lus.Application.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lus.Infrastructure.Configuration
{
    public sealed class DocumentDayEntityTypeConfiguration : IEntityTypeConfiguration<DocumentDay>
    {
        public void Configure(EntityTypeBuilder<DocumentDay> builder)
        {
            AuditableEntityConfiguration.ConfigureAudit(builder);
            builder.ToTable("DocumentDays");
            builder.Property(d => d.Date).IsRequired();
            builder.Property(d => d.DayOfWeek).IsRequired();
            builder.Property(d => d.TotalHours).HasColumnType("decimal(12,2)");

            builder.HasMany(d => d.Rows)
                .WithOne(r => r.Day)
                .HasForeignKey(r => r.DayId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
