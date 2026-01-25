using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

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
