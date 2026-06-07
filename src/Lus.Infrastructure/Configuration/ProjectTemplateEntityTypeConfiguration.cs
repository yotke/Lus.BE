using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lus.Application.Contacts.Entities;
using Lus.Application.ProjectsTemplates.Entities;

namespace Lus.Infrastructure.Configuration
{
    public sealed class ProjectTemplateEntityTypeConfiguration : IEntityTypeConfiguration<ProjectTemplate>
    {
        public void Configure(EntityTypeBuilder<ProjectTemplate> builder)
        {
            builder.HasKey(v => v.Id);

            builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
            builder.Property(t => t.ProjectNumber).IsRequired(false);
            builder.Property(t => t.SectionName).IsRequired().HasMaxLength(350);
            builder.Property(t => t.CurrentDate).IsRequired();
            builder.Property(t => t.ProjectLocation).IsRequired().HasMaxLength(300);
            builder.Property(t => t.ConstrctorName).IsRequired().HasMaxLength(300);
            builder.Property(t => t.AccountNumber).IsRequired().HasMaxLength(300);
            builder.Property(t => t.ProjectSubject).IsRequired();
            builder.Property(t => t.WorkKindRate).IsRequired(false);
            builder.Property(t => t.WorkRate).IsRequired(false);
            builder.Property(t => t.WorkerName).IsRequired();
            builder.Property(t => t.StartContractDate).IsRequired();
            builder.Property(t => t.EndContractDate).IsRequired();
            builder.Property(t => t.WorkContractNumber).IsRequired().HasMaxLength(300);
            builder.Property(t => t.EmployeeSectionName).IsRequired().HasMaxLength(300);

            builder.Property(t => t.ConstrctorPhone).IsRequired().HasMaxLength(300);
            builder.Property(t => t.ConstrctorTitle).IsRequired().HasMaxLength(500);
            builder.Property(t => t.ConstrctorAddress).IsRequired().HasMaxLength(500);
            builder.Property(t => t.ProjectManager).IsRequired(false).HasMaxLength(500);
            builder.Property(t => t.ConstrctorEntrepreneurNumber).IsRequired().HasMaxLength(500);


        builder.HasOne(d => d.Organization)
                .WithMany(a => a.ProjectsTemplates)
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
