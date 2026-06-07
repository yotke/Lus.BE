using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Users.Entities;
using Lus.Infrastructure.ValuesConverters;

namespace Lus.Infrastructure.Configuration
{
    public sealed class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(conf => conf.Email).IsUnique();
            builder.HasIndex(conf => conf.IdNumber).IsUnique();

            builder.Property(u => u.Email).IsRequired().HasMaxLength(500);
            builder.Property(u => u.FirstName).IsRequired().HasMaxLength(150);
            builder.Property(u => u.LastName).IsRequired().HasMaxLength(150);
            builder.Property(u => u.IsConfirmed).IsRequired().HasDefaultValue(false);
            builder.Property(u => u.PasswordVerificationToken).IsRequired(false).HasMaxLength(500);
            builder.Property(u => u.SmsVerificationToken).IsRequired(false).HasMaxLength(500);
            builder.Property(u => u.LastPasswordFailureDate).IsRequired(false);
            builder.Property(u => u.VerificationTokenExpiration).IsRequired(false);
            builder.Property(u => u.SmsTokenExpiration).IsRequired(false);
            builder.Property(u => u.PasswordChangedDate).IsRequired(false);
            builder.Property(u => u.ConfirmationToken).IsRequired(false).HasMaxLength(500);
            builder.Property(u => u.Phone).IsRequired(false).HasMaxLength(500);
            builder.Property(u => u.IdNumber).IsRequired().HasMaxLength(20);

            builder.Property(u => u.PasswordHash).HasMaxLength(1000);

            builder.Property(u => u.ClientSecrets).HasConversion(new ListToStringConverter()).HasMaxLength(1000);
            builder.Property(u => u.AllowedGrantTypes).HasConversion(new ListToStringConverter()).HasMaxLength(1000);
            builder.Property(u => u.AllowedScopes).HasConversion(new ListToStringConverter()).HasMaxLength(1000);
            builder.Property(u => u.Claims).HasConversion(new ListOfKeyValuePairToStringConverter());

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
