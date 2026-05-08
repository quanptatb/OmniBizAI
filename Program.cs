using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContextService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<OperationRequestService>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<AiInsightService>();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

// ── Database ─────────────────────────────────────────────────────────────────
// Apply EF Core migrations on startup.
// After migration, fix NULL passwords for SQL-seeded Identity users.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Fix passwords for users seeded via SQL (SQL can't hash ASP.NET Identity passwords)
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
    var usersWithoutPassword = await db.Users.Where(u => u.PasswordHash == null).ToListAsync();
    foreach (var user in usersWithoutPassword)
    {
        await userManager.AddPasswordAsync(user, "123");
    }
}

app.Run();
