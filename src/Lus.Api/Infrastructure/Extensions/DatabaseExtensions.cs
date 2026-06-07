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
            var connectionString = ResolveMySqlConnectionString(configuration);

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

        public static string ResolveMySqlConnectionString(IConfiguration configuration)
        {
            var url = FirstConfiguredValue(configuration, "MYSQL_URL", "MYSQL_PUBLIC_URL");
            if (!string.IsNullOrWhiteSpace(url))
            {
                return ConvertMySqlUrlToConnectionString(url);
            }

            var host = FirstConfiguredValue(configuration, "MYSQLHOST", "MYSQL_HOST");
            var database = FirstConfiguredValue(configuration, "MYSQLDATABASE", "MYSQL_DATABASE", "MYSQL_DATABASE_NAME");
            var user = FirstConfiguredValue(configuration, "MYSQLUSER", "MYSQL_USER");
            var password = FirstConfiguredValue(configuration, "MYSQLPASSWORD", "MYSQL_PASSWORD", "MYSQL_ROOT_PASSWORD");
            var port = FirstConfiguredValue(configuration, "MYSQLPORT", "MYSQL_PORT") ?? "3306";

            if (!string.IsNullOrWhiteSpace(host)
                && !string.IsNullOrWhiteSpace(database)
                && !string.IsNullOrWhiteSpace(user)
                && password != null)
            {
                return $"Server={host};Port={port};Database={database};User={user};Password={password};SslMode=None;AllowPublicKeyRetrieval=true;";
            }

            var fallback = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(fallback))
            {
                throw new InvalidOperationException(
                    "MySQL connection is not configured. Set MYSQL_URL, MYSQL_PUBLIC_URL, MYSQLHOST/MYSQLDATABASE/MYSQLUSER/MYSQLPASSWORD, or ConnectionStrings:DefaultConnection.");
            }

            return fallback;
        }

        private static string ConvertMySqlUrlToConnectionString(string mysqlUrl)
        {
            var uri = new Uri(mysqlUrl);
            var userInfo = uri.UserInfo.Split(':', 2);
            var user = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty);
            var password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty);
            var database = uri.AbsolutePath.TrimStart('/');
            var port = uri.Port > 0 ? uri.Port : 3306;

            return $"Server={uri.Host};Port={port};Database={database};User={user};Password={password};SslMode=None;AllowPublicKeyRetrieval=true;";
        }

        private static string FirstConfiguredValue(IConfiguration configuration, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = configuration[key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
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
