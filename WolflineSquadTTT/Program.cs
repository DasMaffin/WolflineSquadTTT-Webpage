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

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(
                    connStr,
                    ServerVersion.AutoDetect(connStr)
                )
            );

            Assembly serviceAssembly = Assembly.GetExecutingAssembly();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IUserRightService, UserRightService>();
            builder.Services.AddScoped<IPollService, PollService>();
            builder.Services.AddScoped<IRewardService, RewardService>();
            builder.Services.AddHttpClient<ISteamService, SteamService>();
            builder.Services.AddSingleton<DataWriterService>();
            builder.Services.AddSingleton<ILoginCookieService, LoginCookieService>();

            // Persist Data Protection keys so login cookies survive app restarts/redeploys.
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(
                    Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")))
                .SetApplicationName("WolflineSquadTTT");

            builder.Services.AddMemoryCache();
            WebApplication app = builder.Build();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseMiddleware<LoginCookieMiddleware>();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
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