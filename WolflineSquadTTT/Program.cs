using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using WolflineSquadTTT;

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

builder.Services.AddScoped<WolflineSquadTTT.Services.IUserService, WolflineSquadTTT.Services.UserService>();

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
