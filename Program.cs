using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;

// Load .env file before building the host so environment variables
// are available to the Configuration system via AddEnvironmentVariables().
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

// Ensure environment variables (including those from .env) are in the config pipeline.
// The double-underscore (__) convention maps to config hierarchy (:).
// e.g. ConnectionStrings__DefaultConnection → ConnectionStrings:DefaultConnection
builder.Configuration.AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Check your .env file.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = builder.Configuration.GetValue("Security:RequireConfirmedAccount", false))
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

