using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.HtmlTemplates.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class HtmlTemplateEntityTypeConfiguration : IEntityTypeConfiguration<HtmlTemplate>
    {
        public void Configure(EntityTypeBuilder<HtmlTemplate> builder)
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Name).IsRequired().HasMaxLength(150);
            builder.Property(v => v.OrganizationId).IsRequired(false);
            builder.Property(v => v.TemplateData).IsRequired().HasColumnType("LONGTEXT");
            builder.Property(v => v.Subject).IsRequired();
            builder.Property(v => v.ReplayEmail).IsRequired(false);
            builder.Property(v => v.HtmlType).IsRequired();
            builder.Property(v => v.Active).IsRequired(false);

            builder.HasOne(d => d.Organization)
                .WithMany(a => a.HtmlTemplates)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(u => u.CreatedById).ValueGeneratedNever();
            builder.Property(u => u.CreatedOn).ValueGeneratedOnAdd();

            builder.Property(u => u.UpdatedOn).ValueGeneratedOnAdd();
            builder.Property(u => u.UpdatedById).ValueGeneratedNever();

            builder.Property(u => u.DeletedById).ValueGeneratedNever();
            builder.Property(u => u.DeletedOn).ValueGeneratedNever();
            builder.Property(u => u.IsDeleted).HasDefaultValue(false);
            builder.Property(u => u.RowVersion).IsRequired(false);
        }
    }
}
