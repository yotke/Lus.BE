using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Users.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class UserLoginInfoEntityTypeConfiguration : IEntityTypeConfiguration<UserLoginInfo>
    {
        public void Configure(EntityTypeBuilder<UserLoginInfo> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.IpAddress).IsRequired(false).HasMaxLength(100);
            builder.Property(u => u.UserAgent).IsRequired(false).HasMaxLength(500);
            builder.Property(u => u.UserId).IsRequired();

            builder.HasOne(d => d.User)
                .WithMany(a => a.UserLoginInfos)
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
