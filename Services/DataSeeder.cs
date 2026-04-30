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
    }
}
