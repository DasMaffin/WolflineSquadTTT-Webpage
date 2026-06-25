using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WolflineSquadTTT.Infrastructure;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static void Main(string[] args)
        {

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            string? connStr;

            if (builder.Environment.IsDevelopment())
            {
                connStr = builder.Configuration.GetConnectionString("DevDb");
            }
            else
            {
                connStr = builder.Configuration.GetConnectionString("ProdDb");
            }

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSession();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<MySqlConnection>(_ =>
                new MySqlConnection(connStr));

            // Explicit version instead of ServerVersion.AutoDetect: AutoDetect opens a DB connection just to
            // configure EF, so a down/absent SQL server would fail before the app could even serve the error
            // page. With a fixed version, EF only touches the DB for real queries (all behind the exception
            // handler). The instance is MariaDB 11.5.2 — bump this if the server is upgraded.
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(
                    connStr,
                    new MariaDbServerVersion(new Version(11, 5, 2))
                )
            );

            Assembly serviceAssembly = Assembly.GetExecutingAssembly();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IUserRightService, UserRightService>();
            builder.Services.AddScoped<IPollService, PollService>();
            builder.Services.AddScoped<IRewardService, RewardService>();
            builder.Services.AddScoped<IPointShopService, PointShopService>();
            builder.Services.AddScoped<IMarketService, MarketService>();
            builder.Services.AddHostedService<AuctionCloserService>();
            builder.Services.AddSingleton<ISteamNameCache, SteamNameCache>();
            builder.Services.AddHttpClient<ISteamService, SteamService>();
            builder.Services.AddSingleton<DataWriterService>();
            builder.Services.AddSingleton<ILoginCookieService, LoginCookieService>();
            builder.Services.AddSingleton<IGmodAuthTokenService, GmodAuthTokenService>();
            builder.Services.AddSingleton<IGmodSocketHub, GmodSocketHub>();

            // Persist Data Protection keys so login cookies survive app restarts/redeploys.
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(
                    Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")))
                .SetApplicationName("WolflineSquadTTT");

            builder.Services.AddMemoryCache();
            WebApplication app = builder.Build();

            // Exception handling wraps everything below it (incl. the DB-touching LoginCookieMiddleware) so any
            // failure — even a fully unreachable SQL server — renders the styled /Home/Error page with the real
            // exception message. Active in all environments (it replaces the default dev exception page; the page
            // itself shows the full stack trace only in Development) so it's verifiable locally too.
            app.UseExceptionHandler("/Home/Error");

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseSession();
            app.UseMiddleware<LoginCookieMiddleware>();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapControllers();

            app.Run();
        }
    }
}