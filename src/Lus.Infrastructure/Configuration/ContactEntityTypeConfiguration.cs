using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Contacts.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class ContactEntityTypeConfiguration : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
            builder.HasKey(v => v.Id);

            builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
            builder.Property(t => t.Phone).IsRequired().HasMaxLength(200);
            builder.Property(t => t.Email).IsRequired().HasMaxLength(350);
            builder.Property(t => t.IdNumber).IsRequired().HasMaxLength(200);

            builder.HasOne(d => d.Organization)
                .WithMany(a => a.Contacts)
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
