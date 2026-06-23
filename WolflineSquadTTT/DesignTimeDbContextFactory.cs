using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WolflineSquadTTT
{
    // Used by EF Core tooling. It loads the same connection string the app uses so
    // "dotnet ef database update" can connect, but pins a fixed ServerVersion so
    // "dotnet ef migrations add" never needs a live database (no AutoDetect).
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            string environment = GetEnvironment(args);

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            string? connStr = environment == "Development"
                ? config.GetConnectionString("DevDb")
                : config.GetConnectionString("ProdDb");

            DbContextOptionsBuilder<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>();
            MySqlServerVersion version = new MySqlServerVersion(new Version(8, 0, 0));

            if (string.IsNullOrEmpty(connStr))
                options.UseMySql(version); // offline: enough to scaffold migrations
            else
                options.UseMySql(connStr, version);

            return new AppDbContext(options.Options);
        }

        private static string GetEnvironment(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--environment")
                    return args[i + 1];
            }

            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        }
    }
}
