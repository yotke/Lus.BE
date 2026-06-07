using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Users.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class UserPasswordHistoryEntityTypeConfiguration : IEntityTypeConfiguration<UserPasswordHistory>
    {
        public void Configure(EntityTypeBuilder<UserPasswordHistory> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.PasswordHash).IsRequired();

            builder.HasOne(d => d.User)
                .WithMany(a => a.UserPasswordHistories)
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
