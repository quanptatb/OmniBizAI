using Microsoft.EntityFrameworkCore;
using OmniBizAI.Application.Interfaces;
using OmniBizAI.Domain.Entities.Finance;
using OmniBizAI.Domain.Entities.Identity;
using OmniBizAI.Domain.Entities.Organization;
using OmniBizAI.Domain.Entities.Performance;
using OmniBizAI.Domain.Entities.Workflow;
using OmniBizAI.Domain.Enums;
using OmniBizAI.Infrastructure.Data;

namespace OmniBizAI.Infrastructure.Seed;

public sealed class SeedDataService : ISeedDataService
{
    private const string DemoPassword = "Test@123456";
    private readonly ApplicationDbContext _dbContext;

    public SeedDataService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        var roles = SeedRoles();
        var permissions = SeedPermissions();
        _dbContext.Roles.AddRange(roles);
        _dbContext.Permissions.AddRange(permissions);
        await _dbContext.SaveChangesAsync(cancellationToken);

        SeedRolePermissions(roles, permissions);
        var company = new Company
        {
            Name = "OmniBiz Demo Company",
            ShortName = "OmniBiz",
            TaxCode = "0312345678",
            Email = "hello@omnibiz.ai",
            DefaultCurrency = "VND"
        };
        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var departments = SeedDepartments(company.Id);
        _dbContext.Departments.AddRange(departments);
        var positions = SeedPositions(company.Id, departments);
        _dbContext.Positions.AddRange(positions);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var users = SeedUsers();
        _dbContext.Users.AddRange(users);
        await _dbContext.SaveChangesAsync(cancellationToken);

        AssignRoles(users, roles);
        var employees = SeedEmployees(company.Id, users, departments, positions);
        _dbContext.Employees.AddRange(employees);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var marketingManager = employees.First(x => x.Email == "manager.mkt@omnibiz.ai");
        departments.First(x => x.Code == "MKT").ManagerId = marketingManager.Id;

        var fiscalPeriod = new FiscalPeriod
        {
            CompanyId = company.Id,
            Name = "Thang 4/2026",
            Type = "Monthly",
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 4, 30)
        };
        var evaluationPeriod = new EvaluationPeriod
        {
            CompanyId = company.Id,
            Name = "Q2/2026",
            Type = "Quarterly",
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 6, 30),
            Status = "Active"
        };
        _dbContext.FiscalPeriods.Add(fiscalPeriod);
        _dbContext.EvaluationPeriods.Add(evaluationPeriod);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var categories = SeedCategories(company.Id);
        _dbContext.BudgetCategories.AddRange(categories);
        var wallet = new Wallet
        {
            CompanyId = company.Id,
            Name = "Tai khoan ngan hang chinh",
            Type = "BankAccount",
            Balance = 1_500_000_000,
            Currency = "VND",
            BankName = "VCB",
            AccountNumber = "0123456789"
        };
        _dbContext.Wallets.Add(wallet);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var budgets = SeedBudgets(company.Id, fiscalPeriod.Id, departments, categories);
        _dbContext.Budgets.AddRange(budgets);
        var vendors = SeedVendors(company.Id);
        _dbContext.Vendors.AddRange(vendors);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var transactions = SeedTransactions(company.Id, wallet.Id, departments, categories, budgets);
        _dbContext.Transactions.AddRange(transactions);
        foreach (var expense in transactions.Where(x => x.Type == TransactionType.Expense && x.BudgetId.HasValue))
        {
            budgets.First(x => x.Id == expense.BudgetId).SpentAmount += expense.Amount;
            wallet.Balance -= expense.Amount;
        }
        foreach (var income in transactions.Where(x => x.Type == TransactionType.Income))
        {
            wallet.Balance += income.Amount;
        }

        SeedPerformance(company.Id, evaluationPeriod.Id, departments, employees);
        SeedWorkflow(company.Id, roles);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<Role> SeedRoles()
    {
        return
        [
            new Role { Name = "Admin", DisplayName = "Quan tri he thong", Level = 1 },
            new Role { Name = "Director", DisplayName = "Giam doc", Level = 2 },
            new Role { Name = "Manager", DisplayName = "Truong phong", Level = 3 },
            new Role { Name = "Accountant", DisplayName = "Ke toan", Level = 4 },
            new Role { Name = "HR", DisplayName = "Nhan su", Level = 4 },
            new Role { Name = "Staff", DisplayName = "Nhan vien", Level = 5 }
        ];
    }

    private static List<Permission> SeedPermissions()
    {
        string[] modules = ["user", "finance", "kpi", "workflow", "ai", "report", "audit", "settings", "hr"];
        string[] actions = ["create", "read", "update", "delete", "approve", "export", "manage", "chat"];
        return modules.SelectMany(module => actions.Select(action => new Permission
        {
            Module = module,
            Action = action,
            Resource = module
        })).ToList();
    }

    private void SeedRolePermissions(IReadOnlyCollection<Role> roles, IReadOnlyCollection<Permission> permissions)
    {
        foreach (var role in roles)
        {
            var allowed = permissions.Where(permission =>
                role.Name == "Admin" ||
                role.Name == "Director" && permission.Module is not "settings" ||
                role.Name == "Manager" && permission.Module is "finance" or "kpi" or "workflow" or "ai" or "report" ||
                role.Name == "Accountant" && permission.Module is "finance" or "ai" or "report" ||
                role.Name == "HR" && permission.Module is "hr" or "user" or "ai" ||
                role.Name == "Staff" && permission.Action is "read" or "create" or "chat").ToList();

            _dbContext.RolePermissions.AddRange(allowed.Select(permission => new RolePermission { RoleId = role.Id, PermissionId = permission.Id }));
        }
    }

    private static List<Department> SeedDepartments(Guid companyId)
    {
        return
        [
            new Department { CompanyId = companyId, Name = "Ban Giam Doc", Code = "BOD", BudgetLimit = 500_000_000 },
            new Department { CompanyId = companyId, Name = "Marketing", Code = "MKT", BudgetLimit = 150_000_000 },
            new Department { CompanyId = companyId, Name = "Sales", Code = "SAL", BudgetLimit = 120_000_000 },
            new Department { CompanyId = companyId, Name = "Finance", Code = "FIN", BudgetLimit = 80_000_000 },
            new Department { CompanyId = companyId, Name = "Human Resources", Code = "HR", BudgetLimit = 60_000_000 },
            new Department { CompanyId = companyId, Name = "Information Technology", Code = "IT", BudgetLimit = 100_000_000 }
        ];
    }

    private static List<Position> SeedPositions(Guid companyId, IReadOnlyCollection<Department> departments)
    {
        return
        [
            new Position { CompanyId = companyId, Name = "Director", Level = 1, DepartmentId = departments.First(x => x.Code == "BOD").Id },
            new Position { CompanyId = companyId, Name = "Manager", Level = 2 },
            new Position { CompanyId = companyId, Name = "Senior Specialist", Level = 3 },
            new Position { CompanyId = companyId, Name = "Staff", Level = 4 }
        ];
    }

    private static List<User> SeedUsers()
    {
        string[] emails =
        [
            "admin@omnibiz.ai",
            "director@omnibiz.ai",
            "manager.mkt@omnibiz.ai",
            "manager.it@omnibiz.ai",
            "accountant@omnibiz.ai",
            "hr@omnibiz.ai",
            "staff.mkt@omnibiz.ai",
            "staff.sales@omnibiz.ai"
        ];

        return emails.Select(email => new User
        {
            Email = email,
            FullName = ToDisplayName(email),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 12),
            EmailConfirmed = true
        }).ToList();
    }

    private void AssignRoles(IReadOnlyCollection<User> users, IReadOnlyCollection<Role> roles)
    {
        var map = new Dictionary<string, string>
        {
            ["admin@omnibiz.ai"] = "Admin",
            ["director@omnibiz.ai"] = "Director",
            ["manager.mkt@omnibiz.ai"] = "Manager",
            ["manager.it@omnibiz.ai"] = "Manager",
            ["accountant@omnibiz.ai"] = "Accountant",
            ["hr@omnibiz.ai"] = "HR",
            ["staff.mkt@omnibiz.ai"] = "Staff",
            ["staff.sales@omnibiz.ai"] = "Staff"
        };
        foreach (var user in users)
        {
            _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roles.First(x => x.Name == map[user.Email]).Id });
        }
    }

    private static List<Employee> SeedEmployees(Guid companyId, IReadOnlyCollection<User> users, IReadOnlyCollection<Department> departments, IReadOnlyCollection<Position> positions)
    {
        Department Dept(string code) => departments.First(x => x.Code == code);
        Position Pos(string name) => positions.First(x => x.Name == name);

        return users.Select((user, index) => new Employee
        {
            CompanyId = companyId,
            UserId = user.Id,
            EmployeeCode = $"EMP-{index + 1:000}",
            FullName = user.FullName,
            Email = user.Email,
            DepartmentId = user.Email switch
            {
                "manager.mkt@omnibiz.ai" or "staff.mkt@omnibiz.ai" => Dept("MKT").Id,
                "manager.it@omnibiz.ai" => Dept("IT").Id,
                "accountant@omnibiz.ai" => Dept("FIN").Id,
                "hr@omnibiz.ai" => Dept("HR").Id,
                "staff.sales@omnibiz.ai" => Dept("SAL").Id,
                _ => Dept("BOD").Id
            },
            PositionId = user.Email.Contains("manager") ? Pos("Manager").Id : user.Email is "director@omnibiz.ai" ? Pos("Director").Id : Pos("Staff").Id,
            JoinDate = new DateOnly(2024, 1, 15)
        }).ToList();
    }

    private static List<BudgetCategory> SeedCategories(Guid companyId)
    {
        return
        [
            new BudgetCategory { CompanyId = companyId, Name = "Doanh thu ban hang", Code = "REV-SALES", Type = TransactionType.Income, Color = "#16A34A" },
            new BudgetCategory { CompanyId = companyId, Name = "Marketing Ads", Code = "MKT-ADS", Type = TransactionType.Expense, Color = "#2563EB" },
            new BudgetCategory { CompanyId = companyId, Name = "Phan mem va ha tang", Code = "IT-SW", Type = TransactionType.Expense, Color = "#7C3AED" },
            new BudgetCategory { CompanyId = companyId, Name = "Van phong", Code = "OFFICE", Type = TransactionType.Expense, Color = "#EA580C" }
        ];
    }

    private static List<Budget> SeedBudgets(Guid companyId, Guid fiscalPeriodId, IReadOnlyCollection<Department> departments, IReadOnlyCollection<BudgetCategory> categories)
    {
        return
        [
            new Budget { CompanyId = companyId, FiscalPeriodId = fiscalPeriodId, DepartmentId = departments.First(x => x.Code == "MKT").Id, CategoryId = categories.First(x => x.Code == "MKT-ADS").Id, Name = "Marketing Ads T4/2026", AllocatedAmount = 70_000_000 },
            new Budget { CompanyId = companyId, FiscalPeriodId = fiscalPeriodId, DepartmentId = departments.First(x => x.Code == "IT").Id, CategoryId = categories.First(x => x.Code == "IT-SW").Id, Name = "IT Software T4/2026", AllocatedAmount = 40_000_000 },
            new Budget { CompanyId = companyId, FiscalPeriodId = fiscalPeriodId, DepartmentId = departments.First(x => x.Code == "FIN").Id, CategoryId = categories.First(x => x.Code == "OFFICE").Id, Name = "Finance Office T4/2026", AllocatedAmount = 25_000_000 }
        ];
    }

    private static List<Vendor> SeedVendors(Guid companyId)
    {
        return
        [
            new Vendor { CompanyId = companyId, Name = "Google Vietnam", TaxCode = "0100112233", Email = "billing@google.example", Rating = 4.8m },
            new Vendor { CompanyId = companyId, Name = "Microsoft Vietnam", TaxCode = "0100445566", Email = "billing@microsoft.example", Rating = 4.7m }
        ];
    }

    private static List<Transaction> SeedTransactions(Guid companyId, Guid walletId, IReadOnlyCollection<Department> departments, IReadOnlyCollection<BudgetCategory> categories, IReadOnlyCollection<Budget> budgets)
    {
        return
        [
            new Transaction { CompanyId = companyId, TransactionNumber = "TXN-2026-0001", Type = TransactionType.Income, Amount = 1_200_000_000, WalletId = walletId, DepartmentId = departments.First(x => x.Code == "SAL").Id, CategoryId = categories.First(x => x.Code == "REV-SALES").Id, TransactionDate = new DateOnly(2026, 4, 3), Description = "Doanh thu thang 4" },
            new Transaction { CompanyId = companyId, TransactionNumber = "TXN-2026-0002", Type = TransactionType.Expense, Amount = 85_000_000, WalletId = walletId, DepartmentId = departments.First(x => x.Code == "MKT").Id, CategoryId = categories.First(x => x.Code == "MKT-ADS").Id, BudgetId = budgets.First(x => x.Name.StartsWith("Marketing")).Id, TransactionDate = new DateOnly(2026, 4, 8), Description = "Quang cao da kenh" },
            new Transaction { CompanyId = companyId, TransactionNumber = "TXN-2026-0003", Type = TransactionType.Expense, Amount = 30_000_000, WalletId = walletId, DepartmentId = departments.First(x => x.Code == "IT").Id, CategoryId = categories.First(x => x.Code == "IT-SW").Id, BudgetId = budgets.First(x => x.Name.StartsWith("IT")).Id, TransactionDate = new DateOnly(2026, 4, 10), Description = "License phan mem" }
        ];
    }

    private void SeedPerformance(Guid companyId, Guid periodId, IReadOnlyCollection<Department> departments, IReadOnlyCollection<Employee> employees)
    {
        var objective = new Objective
        {
            CompanyId = companyId,
            PeriodId = periodId,
            Title = "Tang truong doanh thu ben vung Q2",
            OwnerType = OwnerType.Department,
            DepartmentId = departments.First(x => x.Code == "SAL").Id,
            Status = "Active",
            Progress = 65
        };
        var keyResult = new KeyResult
        {
            Objective = objective,
            Title = "Dat 3 ty VND doanh thu moi",
            MetricType = MetricType.Currency,
            Unit = "VND",
            TargetValue = 3_000_000_000,
            CurrentValue = 1_950_000_000,
            Progress = 65,
            Weight = 100
        };
        var kpi = new Kpi
        {
            CompanyId = companyId,
            PeriodId = periodId,
            Name = "Marketing qualified leads",
            DepartmentId = departments.First(x => x.Code == "MKT").Id,
            AssigneeId = employees.First(x => x.Email == "staff.mkt@omnibiz.ai").Id,
            MetricType = MetricType.Number,
            StartValue = 0,
            TargetValue = 500,
            CurrentValue = 360,
            Progress = 72,
            Weight = 40,
            Rating = "B",
            Status = "Active"
        };
        _dbContext.Objectives.Add(objective);
        _dbContext.KeyResults.Add(keyResult);
        _dbContext.Kpis.Add(kpi);
    }

    private void SeedWorkflow(Guid companyId, IReadOnlyCollection<Role> roles)
    {
        var template = new WorkflowTemplate
        {
            CompanyId = companyId,
            Name = "Duyet de nghi chi mac dinh",
            EntityType = "PaymentRequest",
            IsActive = true,
            IsDefault = true,
            Steps =
            {
                new WorkflowStep { StepOrder = 1, Name = "Quan ly phong ban duyet", ApproverType = "Role", ApproverRoleId = roles.First(x => x.Name == "Manager").Id },
                new WorkflowStep { StepOrder = 2, Name = "Giam doc duyet", ApproverType = "Role", ApproverRoleId = roles.First(x => x.Name == "Director").Id }
            }
        };
        _dbContext.WorkflowTemplates.Add(template);
    }

    private static string ToDisplayName(string email)
    {
        return email.Split('@')[0].Replace('.', ' ').Replace("mkt", "Marketing").Replace("it", "IT");
    }
}
