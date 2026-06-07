using Hangfire;
using Hangfire.MySql;
using Lus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;


namespace Lus.Infrastructure.Extensions
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Use MySQL for the application database context
            services.AddDbContext<ApplicationContext>(
         options => options.UseMySql(
             connectionString,
             ServerVersion.AutoDetect(connectionString)
         )
     );

            // Assuming Hangfire supports MySQL, adjust for MySQL storage. You might need a different package for this.
            var storageOptions = new MySqlStorageOptions
            {
                // Set any properties that are relevant for your setup
                // For example: 
                // TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                // QueuePollInterval = TimeSpan.FromSeconds(15),
                // JobExpirationCheckInterval = TimeSpan.FromHours(1),
                // CountersAggregateInterval = TimeSpan.FromMinutes(5),
                // ...
            };

            // Assuming Hangfire supports MySQL, adjust for MySQL storage.
            services.AddHangfire(x => x.UseStorage(new MySqlStorage(connectionString, storageOptions)));
            services.AddHangfireServer();

            return services;
        }

        public static void ApplyMigrations(this IApplicationBuilder app, IConfiguration configuration)
        {
            if (configuration.GetValue<bool>("General:AutoMigrations"))
            {
                using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                    context.Database.Migrate();
                }
            }
        }
    }
}