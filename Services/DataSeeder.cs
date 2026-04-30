using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using System.Security.Claims;

namespace OmniBizAI.Services;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Apply migrations
        await context.Database.MigrateAsync();

        // Seed Tenant
        var tenantCode = "BIZEN";
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Code == tenantCode);
        if (tenant == null)
        {
            tenant = new Tenant
            {
                Code = tenantCode,
                Name = "Bizen Catering",
                IsActive = true
            };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }

        // Seed Role
        var roleCode = "ADMIN";
        if (!await roleManager.RoleExistsAsync(roleCode))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = roleCode, NormalizedName = roleCode, TenantId = tenant.Id, Description = "System Administrator" });
        }

        // Seed User
        var adminEmail = "admin@bizen.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Administrator",
                TenantId = tenant.Id,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, roleCode);
                
                // Add TenantId claim for context reading
                await userManager.AddClaimAsync(adminUser, new Claim("TenantId", tenant.Id.ToString()));
            }
        }

        // Seed Company
        var company = await context.Companies.FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Code == "BIZEN_CORP");
        if (company == null)
        {
            company = new Company
            {
                TenantId = tenant.Id,
                Code = "BIZEN_CORP",
                Name = "Bizen Catering Services",
                IsActive = true
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();
        }

        // Seed Departments
        var deptCodes = new[] { "BOD", "OPS", "MENU", "KITCHEN", "QA", "PUR", "CS", "ADMIN" };
        foreach (var code in deptCodes)
        {
            var dept = await context.Departments.FirstOrDefaultAsync(d => d.TenantId == tenant.Id && d.Code == code);
            if (dept == null)
            {
                context.Departments.Add(new Department
                {
                    TenantId = tenant.Id,
                    CompanyId = company.Id,
                    Code = code,
                    Name = code + " Department",
                    IsActive = true
                });
            }
        }
        await context.SaveChangesAsync();

        // Seed EmployeeProfile for Admin
        if (adminUser != null)
        {
            var adminDept = await context.Departments.FirstOrDefaultAsync(d => d.TenantId == tenant.Id && d.Code == "ADMIN");
            if (adminDept != null)
            {
                var emp = await context.EmployeeProfiles.FirstOrDefaultAsync(e => e.UserId == adminUser.Id);
                if (emp == null)
                {
                    context.EmployeeProfiles.Add(new EmployeeProfile
                    {
                        UserId = adminUser.Id,
                        DepartmentId = adminDept.Id,
                        PositionName = "System Administrator",
                        EmployeeCode = "EMP0001",
                        JoinDate = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
