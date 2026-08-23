using Lus.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lus.Api.Tests.Infrastructure;

public class DatabaseExtensionsTests
{
    /// <summary>
    /// Splits a connection string into its key/value pairs so a test can assert on the parts it
    /// cares about. A full-string equality assertion here is brittle: adding one option (as the
    /// SslMode/AllowPublicKeyRetrieval pair below was) breaks every test without any behaviour
    /// regression, which is exactly how these two ended up failing on main.
    /// </summary>
    private static Dictionary<string, string> Parse(string connectionString) =>
        connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(kv => kv[0], kv => kv.Length > 1 ? kv[1] : "", StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Railway's MySQL uses caching_sha2_password over a non-TLS internal link, so Pomelo needs
    /// both of these or the connection fails at handshake. Every resolution path must set them.
    /// </summary>
    private static void AssertRailwayDriverOptions(Dictionary<string, string> parts)
    {
        Assert.Equal("None", parts["SslMode"]);
        Assert.Equal("true", parts["AllowPublicKeyRetrieval"]);
    }

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

        var parts = Parse(DatabaseExtensions.ResolveMySqlConnectionString(configuration));

        // The INTERNAL url wins over both the public proxy and the local default — egress through
        // the public proxy is billed and slower.
        Assert.Equal("mysql.railway.internal", parts["Server"]);
        Assert.Equal("3306", parts["Port"]);
        Assert.Equal("railway", parts["Database"]);
        Assert.Equal("root", parts["User"]);
        // Percent-encoded credentials must be decoded: p%40ss -> p@ss.
        Assert.Equal("p@ss", parts["Password"]);
        AssertRailwayDriverOptions(parts);
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

        var parts = Parse(DatabaseExtensions.ResolveMySqlConnectionString(configuration));

        Assert.Equal("mysql.railway.internal", parts["Server"]);
        Assert.Equal("3306", parts["Port"]);
        Assert.Equal("railway", parts["Database"]);
        Assert.Equal("root", parts["User"]);
        Assert.Equal("secret", parts["Password"]);
        AssertRailwayDriverOptions(parts);
    }

    [Fact]
    public void ResolveMySqlConnectionString_FallsBackToDefaultConnection_WhenNoRailwayVarsPresent()
    {
        // Local dev: no Railway variables at all. The configured connection string is returned
        // untouched — the Railway driver options are NOT forced onto a local server.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Port=3306;Database=LusManager;User=root;Password=local;"
            })
            .Build();

        var result = DatabaseExtensions.ResolveMySqlConnectionString(configuration);

        Assert.Equal("Server=localhost;Port=3306;Database=LusManager;User=root;Password=local;", result);
    }
}
