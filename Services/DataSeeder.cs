using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Services.Auth;

namespace OmniBizAI.Services;

public static class DataSeeder
{
    private const string DefaultProfilePath = "Data/Seed/bizen.seed.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        await context.Database.MigrateAsync();

        var profile = await LoadProfileAsync(hostEnvironment, configuration);
        var tenant = await UpsertTenantAsync(context, profile.Tenant);
        var company = await UpsertCompanyAsync(context, tenant.Id, profile.Company);

        await SeedRolesAsync(roleManager, tenant.Id, profile.Roles);
        var departments = await SeedDepartmentsAsync(context, tenant.Id, company.Id, profile.Departments);
        await SeedUsersAsync(context, userManager, tenant.Id, departments, profile, configuration);
    }

    private static async Task<SeedProfile> LoadProfileAsync(IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        var configuredPath = configuration["Seed:ProfilePath"];
        var profilePath = string.IsNullOrWhiteSpace(configuredPath) ? DefaultProfilePath : configuredPath;

        if (!Path.IsPathRooted(profilePath))
        {
            profilePath = Path.Combine(hostEnvironment.ContentRootPath, profilePath);
        }

        if (!File.Exists(profilePath))
        {
            throw new FileNotFoundException($"Seed profile not found: {profilePath}", profilePath);
        }

        await using var stream = File.OpenRead(profilePath);
        return await JsonSerializer.DeserializeAsync<SeedProfile>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Seed profile is empty: {profilePath}");
    }

    private static async Task<Tenant> UpsertTenantAsync(ApplicationDbContext context, SeedTenant seed)
    {
        EnsureValue(seed.Code, "Tenant.Code");
        EnsureValue(seed.Name, "Tenant.Name");

        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Code == seed.Code);
        if (tenant == null)
        {
            tenant = new Tenant
            {
                Code = seed.Code,
                Name = seed.Name,
                IsActive = seed.IsActive
            };
            context.Tenants.Add(tenant);
        }
        else
        {
            tenant.Name = seed.Name;
            tenant.IsActive = seed.IsActive;
        }

        await context.SaveChangesAsync();
        return tenant;
    }

    private static async Task<Company> UpsertCompanyAsync(ApplicationDbContext context, int tenantId, SeedCompany seed)
    {
        EnsureValue(seed.Code, "Company.Code");
        EnsureValue(seed.Name, "Company.Name");

        var company = await context.Companies.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Code == seed.Code);
        if (company == null)
        {
            company = new Company
            {
                TenantId = tenantId,
                Code = seed.Code
            };
            context.Companies.Add(company);
        }

        company.Name = seed.Name;
        company.Address = seed.Address;
        company.TaxCode = seed.TaxCode;
        company.IsActive = seed.IsActive;

        await context.SaveChangesAsync();
        return company;
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, int tenantId, List<SeedRole> roles)
    {
        foreach (var seed in roles)
        {
            EnsureValue(seed.Code, "Role.Code");

            var role = await roleManager.FindByNameAsync(seed.Code);
            if (role == null)
            {
                role = new ApplicationRole
                {
                    Name = seed.Code,
                    TenantId = tenantId,
                    Description = seed.Description
                };
                EnsureSuccess(await roleManager.CreateAsync(role), $"Create role {seed.Code}");
                continue;
            }

            role.TenantId = tenantId;
            role.Description = seed.Description;
            EnsureSuccess(await roleManager.UpdateAsync(role), $"Update role {seed.Code}");
        }
    }

    private static async Task<Dictionary<string, Department>> SeedDepartmentsAsync(
        ApplicationDbContext context,
        int tenantId,
        int companyId,
        List<SeedDepartment> departmentSeeds)
    {
        foreach (var seed in departmentSeeds)
        {
            EnsureValue(seed.Code, "Department.Code");
            EnsureValue(seed.Name, "Department.Name");

            var department = await context.Departments
                .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Code == seed.Code);

            if (department == null)
            {
                department = new Department
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    Code = seed.Code
                };
                context.Departments.Add(department);
            }

            department.Name = seed.Name;
            department.CompanyId = companyId;
            department.IsActive = seed.IsActive;
        }

        await context.SaveChangesAsync();

        var departments = await context.Departments
            .Where(d => d.TenantId == tenantId)
            .ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in departmentSeeds.Where(d => !string.IsNullOrWhiteSpace(d.ParentDepartmentCode)))
        {
            var department = departments[seed.Code];
            department.ParentDepartmentId = departments.TryGetValue(seed.ParentDepartmentCode!, out var parent)
                ? parent.Id
                : null;
        }

        await context.SaveChangesAsync();

        return await context.Departments
            .Where(d => d.TenantId == tenantId)
            .ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task SeedUsersAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        int tenantId,
        Dictionary<string, Department> departments,
        SeedProfile profile,
        IConfiguration configuration)
    {
        var defaultPassword = ResolveDefaultPassword(profile, configuration);

        foreach (var seed in profile.Users)
        {
            var email = ResolveEmail(seed, configuration);
            EnsureValue(email, "User.Email");
            EnsureValue(seed.FullName, $"User.FullName for {email}");
            EnsureValue(seed.RoleCode, $"User.RoleCode for {email}");
            EnsureValue(seed.DepartmentCode, $"User.DepartmentCode for {email}");

            if (!departments.TryGetValue(seed.DepartmentCode, out var department))
            {
                throw new InvalidOperationException($"Department '{seed.DepartmentCode}' not found for user '{email}'.");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = seed.FullName,
                    TenantId = tenantId,
                    IsActive = seed.IsActive
                };

                EnsureSuccess(await userManager.CreateAsync(user, defaultPassword), $"Create user {email}");
            }
            else
            {
                user.UserName = email;
                user.Email = email;
                user.EmailConfirmed = true;
                user.FullName = seed.FullName;
                user.TenantId = tenantId;
                user.IsActive = seed.IsActive;
                EnsureSuccess(await userManager.UpdateAsync(user), $"Update user {email}");
            }

            if (!await userManager.IsInRoleAsync(user, seed.RoleCode))
            {
                EnsureSuccess(await userManager.AddToRoleAsync(user, seed.RoleCode), $"Assign role {seed.RoleCode} to {email}");
            }

            await EnsureTenantClaimAsync(userManager, user, tenantId);
            await UpsertEmployeeProfileAsync(context, user.Id, department.Id, seed);
        }
    }

    private static async Task EnsureTenantClaimAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, int tenantId)
    {
        var tenantValue = tenantId.ToString(CultureInfo.InvariantCulture);
        var claims = await userManager.GetClaimsAsync(user);
        var tenantClaims = claims.Where(c => c.Type == TenantClaimTypes.TenantId).ToList();

        if (tenantClaims.Count == 1 && tenantClaims[0].Value == tenantValue)
        {
            return;
        }

        if (tenantClaims.Count > 0)
        {
            EnsureSuccess(await userManager.RemoveClaimsAsync(user, tenantClaims), $"Refresh tenant claim for {user.Email}");
        }

        EnsureSuccess(
            await userManager.AddClaimAsync(user, new Claim(TenantClaimTypes.TenantId, tenantValue)),
            $"Add tenant claim for {user.Email}");
    }

    private static async Task UpsertEmployeeProfileAsync(
        ApplicationDbContext context,
        string userId,
        int departmentId,
        SeedUser seed)
    {
        var employee = await context.EmployeeProfiles.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null)
        {
            context.EmployeeProfiles.Add(new EmployeeProfile
            {
                UserId = userId,
                DepartmentId = departmentId,
                PositionName = seed.PositionName,
                EmployeeCode = seed.EmployeeCode,
                JoinDate = DateTime.UtcNow
            });
        }
        else
        {
            employee.DepartmentId = departmentId;
            employee.PositionName = seed.PositionName;
            employee.EmployeeCode = seed.EmployeeCode;
            employee.JoinDate ??= DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    private static string ResolveDefaultPassword(SeedProfile profile, IConfiguration configuration)
    {
        var configuredPassword = configuration["Seed:DefaultPassword"];
        if (!string.IsNullOrWhiteSpace(configuredPassword)
            && !configuredPassword.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            return configuredPassword;
        }

        EnsureValue(profile.DefaultPassword, "DefaultPassword");
        return profile.DefaultPassword;
    }

    private static string ResolveEmail(SeedUser seed, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(seed.EmailConfigKey))
        {
            var configuredEmail = configuration[seed.EmailConfigKey];
            if (!string.IsNullOrWhiteSpace(configuredEmail))
            {
                return configuredEmail;
            }
        }

        return seed.Email;
    }

    private static void EnsureValue(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Seed profile value is required: {name}");
        }
    }

    private static void EnsureSuccess(IdentityResult result, string action)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
        throw new InvalidOperationException($"{action} failed: {errors}");
    }

    private sealed class SeedProfile
    {
        public string DefaultPassword { get; set; } = string.Empty;
        public SeedTenant Tenant { get; set; } = new();
        public SeedCompany Company { get; set; } = new();
        public List<SeedRole> Roles { get; set; } = new();
        public List<SeedDepartment> Departments { get; set; } = new();
        public List<SeedUser> Users { get; set; } = new();
    }

    private sealed class SeedTenant
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    private sealed class SeedCompany
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? TaxCode { get; set; }
        public bool IsActive { get; set; } = true;
    }

    private sealed class SeedRole
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    private sealed class SeedDepartment
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentDepartmentCode { get; set; }
        public bool IsActive { get; set; } = true;
    }

    private sealed class SeedUser
    {
        public string EmailConfigKey { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
