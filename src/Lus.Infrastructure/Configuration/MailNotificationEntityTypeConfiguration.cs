using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Notifications.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class MailNotificationEntityTypeConfiguration : IEntityTypeConfiguration<MailNotification>
    {
        public void Configure(EntityTypeBuilder<MailNotification> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.SenderEmail).IsRequired(false).HasMaxLength(500);
            builder.Property(u => u.RecepientEmail).IsRequired(false).HasMaxLength(500);
            builder.Property(u => u.Subject).IsRequired(false).HasMaxLength(500);
            builder.Property(u => u.Phone).IsRequired(false).HasMaxLength(50);
            builder.Property(u => u.MailType).IsRequired(false);
            builder.Property(u => u.FreeText).IsRequired(false).HasColumnType("LONGTEXT");
            builder.Property(u => u.IncludingFiles).HasDefaultValue(false);

            builder.HasOne(d => d.User)
                    .WithMany(a => a.MailNotifications)
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
