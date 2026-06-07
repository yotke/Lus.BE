using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Notifications.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class SmsNotificationEntityTypeConfiguration : IEntityTypeConfiguration<SmsNotification>
    {
        public void Configure(EntityTypeBuilder<SmsNotification> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.PhoneNumber).IsRequired(false).HasMaxLength(250);
            builder.Property(u => u.Message).IsRequired(false).HasMaxLength(500);
            builder.Property(u => u.Response).IsRequired(false).HasMaxLength(2000);

            builder.HasOne(d => d.User)
                    .WithMany(a => a.SmsNotifications)
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
