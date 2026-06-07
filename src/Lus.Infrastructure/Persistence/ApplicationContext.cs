using Microsoft.EntityFrameworkCore;
using Lus.Application.Contacts.Entities;
using Lus.Application.HtmlTemplates.Entities;
using Lus.Application.Images.Entities;
using Lus.Application.Notifications.Entities;
using Lus.Application.Roles.Entities;
using Lus.Application.Users.Entities;
using Lus.Infrastructure.Configuration;

namespace Lus.Infrastructure.Persistence
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.ApplyConfiguration(new HtmlTemplateEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new MailNotificationEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new SmsNotificationEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserLoginAttemptEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserPasswordHistoryEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ContactEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ImageEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserRoleEntityTypeConfiguration());

            modelBuilder.ApplyConfiguration(new UserOrganizationEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserLoginInfoEntityTypeConfiguration());


            AddQueryFilters(modelBuilder);
        }

        public DbSet<User> Users { get; set; }

        public DbSet<UserOrganization> UserOrganizations { get; set; }

 

        public DbSet<UserLoginAttempt> UserLoginAttempts { get; set; }

        public DbSet<UserRecruitmentProcess> UserRecruitmentProcesses { get; set; }

        public DbSet<Contact> Contacts { get; set; }

        public DbSet<UserPasswordHistory> UserPasswordHistories { get; set; }


        public DbSet<MailNotification> MailNotifications { get; set; }

        public DbSet<SmsNotification> SmsNotifications { get; set; }



        public DbSet<HtmlTemplate> HtmlTemplates { get; set; }

        public DbSet<Image> Images { get; set; }



        public DbSet<Role> Roles { get; set; }

        public DbSet<UserRole> UserRoles { get; set; }


        public DbSet<FavoriteUserTender> FavoriteUserTenders { get; set; }

        public DbSet<AppliedUserTender> AppliedUserTenders { get; set; }

        public DbSet<UserLoginInfo> UserLoginInfos { get; set; }

        private void AddQueryFilters(ModelBuilder modelBuilder)
            => modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
    }
}
