using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Users.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class UserRoleEntityTypeConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.HasKey(i => i.Id);
            builder.HasIndex(u => new { u.UserId, u.RoleId });

            builder.HasOne(d => d.Role)
                .WithMany(a => a.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.User)
                .WithMany(a => a.UserRoles)
                .HasForeignKey(d => d.UserId).IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);


            builder.Property(u => u.Active).HasDefaultValue(true);

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
