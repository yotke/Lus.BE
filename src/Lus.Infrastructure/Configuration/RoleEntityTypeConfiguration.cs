using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Roles.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class RoleEntityTypeConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(v => v.Id);

            builder.Property(v => v.Name).IsRequired().HasMaxLength(100);
            builder.Property(t => t.HebrewName).IsRequired(false).HasMaxLength(150);
            builder.Property(t => t.Immunity).IsRequired().HasDefaultValue(0);
            builder.Property(t => t.ShowToAdmin).IsRequired().HasDefaultValue(false);
            builder.Property(t => t.OrganizationId).IsRequired(false);

            builder.HasOne(d => d.Organization)
                .WithMany(a => a.Roles)
                .HasForeignKey(d => d.OrganizationId)
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
