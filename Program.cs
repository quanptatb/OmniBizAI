using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using OmniBizAI.Data;
using OmniBizAI.Services.Integrations;
using OmniBizAI.Services.Options;

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
builder.Services.AddSingleton<IConfigurationStatusService, ConfigurationStatusService>();
builder.Services.AddHttpClient<IAiProviderClient, AiProviderClient>();
builder.Services.AddScoped<OmniBizAI.Services.IEmailService, OmniBizAI.Services.EmailService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireDirector", policy => policy.RequireRole("Admin", "Director"));
    options.AddPolicy("RequireManager", policy => policy.RequireRole("Admin", "Director", "Manager"));
    options.AddPolicy("RequireAccountant", policy => policy.RequireRole("Admin", "Director", "Manager", "Accountant"));
    options.AddPolicy("RequireHR", policy => policy.RequireRole("Admin", "Director", "Manager", "HR"));
    options.AddPolicy("RequireStaff", policy => policy.RequireAuthenticatedUser());
});

var allowedOrigins = builder.Configuration["AllowedOrigins"]?
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true);
        }
    });
});

// Add services to the container.
builder.Services.AddControllersWithViews(options => 
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = ConnectionStringResolver.GetDefaultConnectionString(builder.Configuration);

    options.UseSqlServer(connectionString, sql =>
    {
        sql.MigrationsHistoryTable("__EFMigrationsHistory", "biz");
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DemoIdentitySeeder.SeedAsync(db);
}

if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("ConfiguredOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

static void LoadDotEnv()
{
    var envPath = Path.Combine(AppContext.BaseDirectory, ".env");

    if (!File.Exists(envPath))
    {
        envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    }

    if (!File.Exists(envPath))
    {
        return;
    }

    foreach (var line in File.ReadAllLines(envPath))
    {
        var trimmed = line.Trim();

        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = trimmed.IndexOf('=');

        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');

        if (Environment.GetEnvironmentVariable(key) is null)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
