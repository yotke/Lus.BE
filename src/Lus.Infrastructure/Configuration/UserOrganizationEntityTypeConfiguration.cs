using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Users.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class UserOrganizationEntityTypeConfiguration : IEntityTypeConfiguration<UserOrganization>
    {
        public void Configure(EntityTypeBuilder<UserOrganization> builder)
        {
            builder.HasKey(i => i.Id);
            builder.HasIndex(u => new { u.UserId, u.OrganizationId });

            builder.HasOne(d => d.Organization)
                .WithMany(a => a.UserOrganizations)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.User)
                .WithMany(a => a.UserOrganizations)
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
