using Lus.Application.Common.Services;
using Lus.Application.Users.Entities;
using Lus.Infrastructure.Options;
using Lus.Infrastructure.Services;

namespace Lus.Infrastructure.Extensions
{
    public static class AddUpdateEntitiesConfig
    {
        public static IServiceCollection AddEntityUpdatesConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<IChangeApplierService, ChangeApplierService>();
            services.Configure<UpdateEntitiesOptions>(options =>
            {
                options.ForEntity<User>()
                    .IgnoreProperty(u => u.Id)
                    .IgnoreProperty(u => u.ClientSecrets)
                    .IgnoreProperty(u => u.Claims)
                    .IgnoreProperty(u => u.AllowedGrantTypes)
                    .IgnoreProperty(u => u.AllowedScopes)
                    .IgnoreProperty(u => u.PasswordHash)
                    .IgnoreProperty(u => u.DeletedOn)
                    .IgnoreProperty(u => u.DeletedById)
                    .IgnoreProperty(u => u.CreatedOn)
                    .IgnoreProperty(u => u.CreatedById)
                    .IgnoreProperty(u => u.UpdatedOn)
                    .IgnoreProperty(u => u.UpdatedById);
            });

            return services;
        }
    }
}
