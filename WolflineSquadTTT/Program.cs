using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Reflection;
using WolflineSquadTTT;
using WolflineSquadTTT.Services;

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
builder.Services.AddScoped<IPollOptionService, PollOptionService>();

WebApplication app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();

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


app.Run();
