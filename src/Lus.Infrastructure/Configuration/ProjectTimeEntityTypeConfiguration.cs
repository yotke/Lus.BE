using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.ProjectsTimes.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class ProjectTimeEntityTypeConfiguration : IEntityTypeConfiguration<ProjectTime>
    {
        public void Configure(EntityTypeBuilder<ProjectTime> builder)
        {
            builder.HasKey(v => v.Id);

            builder.Property(v => v.WorkDate).IsRequired();
            builder.Property(v => v.WorkDescription).IsRequired(true).HasColumnType("MEDIUMTEXT");
            builder.Property(v => v.TimeData).IsRequired(false).HasColumnType("LONGTEXT");
            builder.Property(v => v.ProjectTemplateId).IsRequired(true);

            builder.HasOne(d => d.ProjectTemplate)
                    .WithMany(a => a.ProjectTimes)
                    .HasForeignKey(d => d.ProjectTemplateId)
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
