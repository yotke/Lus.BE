using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Images.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class ImageEntityTypeConfiguration : IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Name).IsRequired().HasMaxLength(150);
            builder.Property(v => v.ImageType).IsRequired();
            builder.Property(v => v.FileContent).IsRequired(false).HasColumnType("BLOB");
            builder.Property(v => v.OrganizationId).IsRequired(false);
            builder.Property(v => v.UniqueId).IsRequired().HasDefaultValueSql("SYS_GUID()");
            builder.Property(v => v.UserId).IsRequired(false);
            builder.Property(v => v.Status).IsRequired(false);

            builder.HasOne(d => d.Organization)
                .WithMany(a => a.Images)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.User)
                .WithMany(a => a.Images)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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
