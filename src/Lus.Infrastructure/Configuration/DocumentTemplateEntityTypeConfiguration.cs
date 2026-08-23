using Lus.Application.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lus.Infrastructure.Configuration
{
    public sealed class DocumentTemplateEntityTypeConfiguration : IEntityTypeConfiguration<DocumentTemplate>
    {
        public void Configure(EntityTypeBuilder<DocumentTemplate> builder)
        {
            AuditableEntityConfiguration.ConfigureAudit(builder);
            builder.ToTable("DocumentTemplates");
            builder.Property(t => t.Fingerprint).IsRequired().HasMaxLength(128);
            builder.Property(t => t.Rtl).HasDefaultValue(true);
            builder.Property(t => t.ColumnWidths).IsRequired().HasColumnType("LONGTEXT");
            builder.Property(t => t.LetterheadFields).IsRequired().HasColumnType("LONGTEXT");
            builder.Property(t => t.TableHeader).IsRequired().HasColumnType("LONGTEXT");
            builder.Property(t => t.DataBandMergePolicy).IsRequired().HasColumnType("LONGTEXT");
            builder.Property(t => t.TotalsFormulaSet).IsRequired().HasColumnType("LONGTEXT");
            builder.Property(t => t.BillingBlock).IsRequired().HasColumnType("LONGTEXT");
            builder.Property(t => t.DeclarationBlock).IsRequired().HasColumnType("LONGTEXT");
            builder.Property(t => t.ContractBlock).HasColumnType("LONGTEXT");

            builder.HasOne(t => t.Series)
                .WithOne(s => s.Template)
                .HasForeignKey<DocumentTemplate>(t => t.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
