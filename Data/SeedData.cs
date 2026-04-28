using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities;
using System.Data;

namespace OmniBizAI.Data;

public static class SeedData
{
    public static async Task EnsureMinimalFinanceSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (!await RequiredTablesExistAsync(context))
        {
            await context.Database.MigrateAsync();
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var now = DateTime.UtcNow;
        var company = await context.Companies.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (company is null)
        {
            company = new Company
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Name = "OmniBiz Solutions",
                CreatedAt = now
            };
            context.Companies.Add(company);
        }

        if (!await context.Departments.AnyAsync(x => x.CompanyId == company.Id))
        {
            var departments = new[]
            {
                new Department { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), CompanyId = company.Id, Code = "MKT", Name = "Marketing", CreatedAt = now },
                new Department { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), CompanyId = company.Id, Code = "FIN", Name = "Finance", CreatedAt = now },
                new Department { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), CompanyId = company.Id, Code = "OPS", Name = "Operations", CreatedAt = now }
            };

            foreach (var department in departments)
            {
                context.Departments.Add(department);
            }
        }

        if (!await context.BudgetCategories.AnyAsync(x => x.CompanyId == company.Id))
        {
            var categories = new[]
            {
                new BudgetCategory { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), CompanyId = company.Id, Code = "ADS", Name = "Advertising", Type = "Expense", CreatedAt = now },
                new BudgetCategory { Id = Guid.Parse("30000000-0000-0000-0000-000000000002"), CompanyId = company.Id, Code = "CLOUD", Name = "Cloud Services", Type = "Expense", CreatedAt = now },
                new BudgetCategory { Id = Guid.Parse("30000000-0000-0000-0000-000000000003"), CompanyId = company.Id, Code = "OFFICE", Name = "Office Supplies", Type = "Expense", CreatedAt = now }
            };

            foreach (var category in categories)
            {
                context.BudgetCategories.Add(category);
            }
        }

        await context.SaveChangesAsync();

        var demoEmail = "staff@omnibiz.ai";
        var demoUser = await userManager.FindByEmailAsync(demoEmail);
        if (demoUser is null)
        {
            var demoPassword = configuration["DemoSeed:Password"];
            if (!string.IsNullOrWhiteSpace(demoPassword))
            {
                demoUser = new IdentityUser
                {
                    UserName = demoEmail,
                    Email = demoEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(demoUser, demoPassword);
            }
        }

        var marketing = await context.Departments.Where(x => x.CompanyId == company.Id).OrderBy(x => x.CreatedAt).FirstAsync();
        var demoUserGuid = ParseGuidOrNull(demoUser?.Id);
        if (demoUserGuid.HasValue && !await context.Employees.AnyAsync(x => x.UserId == demoUserGuid.Value))
        {
            context.Employees.Add(new Employee
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000001"),
                CompanyId = company.Id,
                DepartmentId = marketing.Id,
                UserId = demoUserGuid.Value,
                EmployeeCode = "EMP-0001",
                FullName = "Demo Staff",
                Email = demoEmail,
                CreatedAt = now
            });
        }

        await context.SaveChangesAsync();
    }

    private static Guid? ParseGuidOrNull(string? value)
    {
        return Guid.TryParse(value, out var result) ? result : null;
    }

    private static async Task<bool> RequiredTablesExistAsync(ApplicationDbContext context)
    {
        var requiredTables = new[]
        {
            "AspNetUsers",
            "Companies",
            "Departments",
            "Employees",
            "BudgetCategories",
            "Vendors",
            "budgets",
            "payment_requests",
            "payment_request_items"
        };

        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            foreach (var table in requiredTables)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $"SELECT CASE WHEN OBJECT_ID(N'{table}', N'U') IS NULL THEN 0 ELSE 1 END";
                var result = await command.ExecuteScalarAsync();
                if (Convert.ToInt32(result) == 0)
                {
                    return false;
                }
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return true;
    }
}
