using Lus.Application.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lus.Infrastructure.Configuration
{
    public sealed class RateCardEntityTypeConfiguration : IEntityTypeConfiguration<RateCard>
    {
        public void Configure(EntityTypeBuilder<RateCard> builder)
        {
            AuditableEntityConfiguration.ConfigureAudit(builder);
            builder.ToTable("RateCards");
            builder.Property(r => r.HourlyRate).HasColumnType("decimal(12,2)");
            builder.Property(r => r.VatPercent).HasColumnType("decimal(5,2)");
            builder.Property(r => r.PlotsPercent).HasColumnType("decimal(5,2)");
        }
    }
}
