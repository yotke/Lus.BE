using Lus.Application.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lus.Infrastructure.Configuration
{
    public sealed class DocumentInstanceEntityTypeConfiguration : IEntityTypeConfiguration<DocumentInstance>
    {
        public void Configure(EntityTypeBuilder<DocumentInstance> builder)
        {
            AuditableEntityConfiguration.ConfigureAudit(builder);
            builder.ToTable("DocumentInstances");
            builder.Property(i => i.SheetName).IsRequired().HasMaxLength(200);
            builder.Property(i => i.AccountNumber).HasMaxLength(64);
            builder.Property(i => i.ProjectName).HasMaxLength(200);
            builder.Property(i => i.ContractNumber).HasMaxLength(64);
            builder.Property(i => i.Status).IsRequired();

            builder.HasOne(i => i.CarryInFrom)
                .WithMany()
                .HasForeignKey(i => i.CarryInFromInstanceId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(i => i.Days)
                .WithOne(d => d.Instance)
                .HasForeignKey(d => d.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
