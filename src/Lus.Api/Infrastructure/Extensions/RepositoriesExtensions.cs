using Lus.Application.Contacts.Repositories;
using Lus.Application.HtmlTemplates.Repositories;
using Lus.Application.Images.Repositories;
using Lus.Application.Notifications.Repositories;
using Lus.Application.Organizations.Repositories;
using Lus.Application.ProjectsTemplates.Repositories;
using Lus.Application.ProjectsTimes.Repositories;
using Lus.Application.Roles.Repositories;
using Lus.Application.Users.Repositories;
using Lus.Infrastructure.Repositories;

namespace Lus.Infrastructure.Extensions
{
    public static class RepositoriesExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUsersRepository, UsersRepository>()
                .AddScoped<IProjectsTemplatesRepository, ProjectsTemplatesRepository>()
                .AddScoped<IUserLoginAttemptsRepository, UserLoginAttemptsRepository>()
                .AddScoped<IUserPasswordHistoriesRepository, UserPasswordHistoriesRepository>()
                .AddScoped<ISmsNotificationsRepository, SmsNotificationsRepository>()
                .AddScoped<IOrganizationsRepository, OrganizationsRepository>()
                .AddScoped<IMailNotificationsRepository, MailNotificationsRepository>()
                .AddScoped<IProjectsTimesRepository, ProjectsTimesRepository>()
                .AddScoped<IUserLoginInfoRepository, UserLoginInfoRepository>()
                .AddScoped<IContactsRepository, ContactsRepository>()

                .AddScoped<IRolesRepository, RolesRepository>()
                .AddScoped<IUserRolesRepository, UserRolesRepository>()
                .AddScoped<IUserOrganizationsRepository, UserOrganizationsRepository>()
                .AddScoped<IImagesRepository, ImagesRepository>()

                .AddScoped<IUserRecruitmentProcessesRepository, UserRecruitmentProcessesRepository>()
                .AddScoped<IHtmlTemplatesRepository, HtmlTemplatesRepository>();

            return services;
        }
    }
}
