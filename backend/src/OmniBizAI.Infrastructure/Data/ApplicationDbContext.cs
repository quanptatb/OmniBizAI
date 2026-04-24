using Microsoft.EntityFrameworkCore;
using OmniBizAI.Domain.Common;
using OmniBizAI.Domain.Entities.AI;
using OmniBizAI.Domain.Entities.Finance;
using OmniBizAI.Domain.Entities.Identity;
using OmniBizAI.Domain.Entities.Notification;
using OmniBizAI.Domain.Entities.Organization;
using OmniBizAI.Domain.Entities.Performance;
using OmniBizAI.Domain.Entities.System;
using OmniBizAI.Domain.Entities.Workflow;

namespace OmniBizAI.Infrastructure.Data;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<UserLoginAttempt> UserLoginAttempts => Set<UserLoginAttempt>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeHistory> EmployeeHistory => Set<EmployeeHistory>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetAdjustment> BudgetAdjustments => Set<BudgetAdjustment>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<PaymentRequest> PaymentRequests => Set<PaymentRequest>();
    public DbSet<PaymentRequestItem> PaymentRequestItems => Set<PaymentRequestItem>();
    public DbSet<PaymentRequestAttachment> PaymentRequestAttachments => Set<PaymentRequestAttachment>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<EvaluationPeriod> EvaluationPeriods => Set<EvaluationPeriod>();
    public DbSet<Objective> Objectives => Set<Objective>();
    public DbSet<KeyResult> KeyResults => Set<KeyResult>();
    public DbSet<Kpi> Kpis => Set<Kpi>();
    public DbSet<KpiCheckIn> KpiCheckIns => Set<KpiCheckIn>();
    public DbSet<PerformanceEvaluation> PerformanceEvaluations => Set<PerformanceEvaluation>();
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowCondition> WorkflowConditions => Set<WorkflowCondition>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowInstanceStep> WorkflowInstanceSteps => Set<WorkflowInstanceStep>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<AiChatSession> AiChatSessions => Set<AiChatSession>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<AiGenerationHistory> AiGenerationHistory => Set<AiGenerationHistory>();
    public DbSet<AiRiskAssessment> AiRiskAssessments => Set<AiRiskAssessment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FileUpload> FileUploads => Set<FileUpload>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");
        ConfigureIdentity(modelBuilder);
        ConfigureOrganization(modelBuilder);
        ConfigureFinance(modelBuilder);
        ConfigurePerformance(modelBuilder);
        ConfigureWorkflow(modelBuilder);
        ConfigureAiAndSystem(modelBuilder);
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).HasMaxLength(255);
            b.Property(x => x.PasswordHash).HasMaxLength(500);
            b.Property(x => x.FullName).HasMaxLength(200);
            b.HasQueryFilter(x => !x.IsDeleted);
        });
        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("Roles");
            b.HasIndex(x => x.Name).IsUnique();
            b.Property(x => x.Name).HasMaxLength(50);
        });
        modelBuilder.Entity<Permission>(b =>
        {
            b.ToTable("Permissions");
            b.HasIndex(x => new { x.Module, x.Action, x.Resource }).IsUnique();
            b.Property(x => x.Module).HasMaxLength(50);
            b.Property(x => x.Action).HasMaxLength(50);
            b.Property(x => x.Resource).HasMaxLength(100);
        });
        modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
        modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.HasIndex(x => x.Token).IsUnique();
            b.Ignore(x => x.IsActive);
        });
        modelBuilder.Entity<UserLoginAttempt>().HasKey(x => x.Id);
    }

    private static void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(b =>
        {
            b.ToTable("Companies");
            b.HasIndex(x => x.TaxCode).IsUnique().HasFilter("[TaxCode] IS NOT NULL");
            b.Property(x => x.Name).HasMaxLength(200);
        });
        modelBuilder.Entity<Department>(b =>
        {
            b.ToTable("Departments");
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Name).HasMaxLength(200);
            b.Property(x => x.Code).HasMaxLength(20);
            b.HasOne(x => x.ParentDepartment).WithMany(x => x.Children).HasForeignKey(x => x.ParentDepartmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => !x.IsDeleted);
        });
        modelBuilder.Entity<Employee>(b =>
        {
            b.ToTable("Employees");
            b.HasIndex(x => x.EmployeeCode).IsUnique();
            b.HasIndex(x => x.Email).IsUnique();
            b.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            b.HasOne(x => x.Manager).WithMany(x => x.DirectReports).HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => !x.IsDeleted);
        });
        modelBuilder.Entity<Position>().ToTable("Positions");
        modelBuilder.Entity<EmployeeHistory>().ToTable("EmployeeHistory");
    }

    private static void ConfigureFinance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FiscalPeriod>().ToTable("FiscalPeriods");
        modelBuilder.Entity<BudgetCategory>(b =>
        {
            b.ToTable("BudgetCategories");
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Budget>(b =>
        {
            b.ToTable("Budgets");
            b.Property(x => x.AllocatedAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.SpentAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.CommittedAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.WarningThreshold).HasColumnType("decimal(5,2)");
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.Ignore(x => x.RemainingAmount);
            b.Ignore(x => x.UtilizationPercent);
            b.Ignore(x => x.WarningLevel);
            b.HasIndex(x => new { x.DepartmentId, x.FiscalPeriodId });
            b.HasQueryFilter(x => !x.IsDeleted);
        });
        modelBuilder.Entity<Vendor>(b =>
        {
            b.ToTable("Vendors");
            b.HasIndex(x => x.TaxCode).IsUnique().HasFilter("[TaxCode] IS NOT NULL");
            b.Property(x => x.Rating).HasColumnType("decimal(3,2)");
            b.HasQueryFilter(x => !x.IsDeleted);
        });
        modelBuilder.Entity<Wallet>(b =>
        {
            b.ToTable("Wallets");
            b.Property(x => x.Balance).HasColumnType("decimal(18,2)");
        });
        modelBuilder.Entity<PaymentRequest>(b =>
        {
            b.ToTable("PaymentRequests");
            b.HasIndex(x => x.RequestNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.AiRiskScore).HasColumnType("decimal(5,2)");
            b.HasQueryFilter(x => !x.IsDeleted);
        });
        modelBuilder.Entity<PaymentRequestItem>(b =>
        {
            b.ToTable("PaymentRequestItems");
            b.Property(x => x.Quantity).HasColumnType("decimal(10,2)");
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.TaxRate).HasColumnType("decimal(5,2)");
            b.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
        });
        modelBuilder.Entity<Transaction>(b =>
        {
            b.ToTable("Transactions");
            b.HasIndex(x => x.TransactionNumber).IsUnique();
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            b.HasQueryFilter(x => !x.IsDeleted);
        });
        modelBuilder.Entity<BudgetAdjustment>().ToTable("BudgetAdjustments");
        modelBuilder.Entity<PaymentRequestAttachment>().ToTable("PaymentRequestAttachments");
    }

    private static void ConfigurePerformance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EvaluationPeriod>().ToTable("EvaluationPeriods");
        modelBuilder.Entity<Objective>(b =>
        {
            b.ToTable("Objectives");
            b.Property(x => x.OwnerType).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Progress).HasColumnType("decimal(5,2)");
            b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
            b.HasQueryFilter(x => !x.IsDeleted);
        });
        modelBuilder.Entity<KeyResult>(b =>
        {
            b.ToTable("KeyResults");
            b.Property(x => x.MetricType).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.StartValue).HasColumnType("decimal(18,2)");
            b.Property(x => x.TargetValue).HasColumnType("decimal(18,2)");
            b.Property(x => x.CurrentValue).HasColumnType("decimal(18,2)");
            b.Property(x => x.Progress).HasColumnType("decimal(5,2)");
            b.Property(x => x.Weight).HasColumnType("decimal(5,2)");
        });
        modelBuilder.Entity<Kpi>(b =>
        {
            b.ToTable("Kpis");
            b.Property(x => x.MetricType).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.TargetValue).HasColumnType("decimal(18,2)");
            b.Property(x => x.CurrentValue).HasColumnType("decimal(18,2)");
            b.Property(x => x.Progress).HasColumnType("decimal(5,2)");
            b.Property(x => x.Weight).HasColumnType("decimal(5,2)");
            b.HasQueryFilter(x => !x.IsDeleted);
        });
        modelBuilder.Entity<KpiCheckIn>(b =>
        {
            b.ToTable("KpiCheckIns");
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
        modelBuilder.Entity<PerformanceEvaluation>().ToTable("PerformanceEvaluations");
    }

    private static void ConfigureWorkflow(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowTemplate>().ToTable("WorkflowTemplates");
        modelBuilder.Entity<WorkflowStep>().ToTable("WorkflowSteps");
        modelBuilder.Entity<WorkflowCondition>().ToTable("WorkflowConditions");
        modelBuilder.Entity<WorkflowInstance>(b =>
        {
            b.ToTable("WorkflowInstances");
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.HasIndex(x => new { x.EntityType, x.EntityId });
        });
        modelBuilder.Entity<WorkflowInstanceStep>().ToTable("WorkflowInstanceSteps");
        modelBuilder.Entity<ApprovalAction>(b =>
        {
            b.ToTable("ApprovalActions");
            b.Property(x => x.Action).HasConversion<string>().HasMaxLength(20);
        });
    }

    private static void ConfigureAiAndSystem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiChatSession>().ToTable("AiChatSessions");
        modelBuilder.Entity<AiMessage>().ToTable("AiMessages");
        modelBuilder.Entity<AiGenerationHistory>().ToTable("AiGenerationHistory");
        modelBuilder.Entity<AiRiskAssessment>(b =>
        {
            b.ToTable("AiRiskAssessments");
            b.Property(x => x.RiskScore).HasColumnType("decimal(5,2)");
        });
        modelBuilder.Entity<Notification>().ToTable("Notifications");
        modelBuilder.Entity<NotificationPreference>().ToTable("NotificationPreferences");
        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("AuditLogs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).UseIdentityColumn();
        });
        modelBuilder.Entity<FileUpload>(b =>
        {
            b.ToTable("FileUploads");
            b.HasQueryFilter(x => !x.IsDeleted);
        });
    }
}
