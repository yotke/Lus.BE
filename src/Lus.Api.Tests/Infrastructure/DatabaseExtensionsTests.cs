using Lus.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lus.Api.Tests.Infrastructure;

public class DatabaseExtensionsTests
{
    [Fact]
    public void ResolveMySqlConnectionString_UsesRailwayInternalUrlFirst()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MYSQL_URL"] = "mysql://root:p%40ss@mysql.railway.internal:3306/railway",
                ["MYSQL_PUBLIC_URL"] = "mysql://root:public@shortline.proxy.rlwy.net:12345/railway",
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=local;"
            })
            .Build();

        var result = DatabaseExtensions.ResolveMySqlConnectionString(configuration);

        Assert.Equal("Server=mysql.railway.internal;Port=3306;Database=railway;User=root;Password=p@ss;", result);
    }

    [Fact]
    public void ResolveMySqlConnectionString_UsesRailwayComponentVariables()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MYSQLHOST"] = "mysql.railway.internal",
                ["MYSQLPORT"] = "3306",
                ["MYSQLDATABASE"] = "railway",
                ["MYSQLUSER"] = "root",
                ["MYSQLPASSWORD"] = "secret"
            })
            .Build();

        var result = DatabaseExtensions.ResolveMySqlConnectionString(configuration);

        Assert.Equal("Server=mysql.railway.internal;Port=3306;Database=railway;User=root;Password=secret;", result);
    }
}
