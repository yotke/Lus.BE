using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lus.Infrastructure.Persistence
{
    /// <summary>
    /// Design-time context factory for <c>dotnet ef migrations add|script</c>.
    ///
    /// WHY THIS EXISTS: the runtime registration in <c>DatabaseExtensions.AddDatabaseContext</c>
    /// uses <c>ServerVersion.AutoDetect(connectionString)</c>, which OPENS A CONNECTION. At design
    /// time that means no migration can be generated without a reachable MySQL — which breaks CI,
    /// breaks a fresh clone, and makes "add a migration" depend on whatever database happens to be
    /// running locally. This factory pins the server version instead of detecting it, so the model
    /// can be built offline.
    ///
    /// It is used ONLY by the EF tooling (<see cref="IDesignTimeDbContextFactory{TContext}"/> is
    /// discovered by the tools, never by the host), so runtime behaviour is unchanged.
    /// </summary>
    public class ApplicationContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationContext>
    {
        /// <summary>
        /// Matches the MySQL 8.0 image in <c>src/docker-compose.yml</c> and Railway's MySQL.
        /// This only shapes the generated SQL dialect — no connection is made.
        /// </summary>
        private static readonly ServerVersion DesignTimeServerVersion = new MySqlServerVersion(new Version(8, 0, 0));

        /// <summary>
        /// Never connected to. A syntactically valid string is required only so the provider can
        /// be configured; override with LUS_DESIGN_TIME_CONNECTION when scaffolding from an
        /// existing database.
        /// </summary>
        private const string PlaceholderConnectionString =
            "Server=localhost;Port=3306;Database=LusManager;User=root;Password=design-time;";

        public ApplicationContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("LUS_DESIGN_TIME_CONNECTION")
                ?? PlaceholderConnectionString;

            var options = new DbContextOptionsBuilder<ApplicationContext>()
                .UseMySql(connectionString, DesignTimeServerVersion)
                .Options;

            return new ApplicationContext(options);
        }
    }
}
