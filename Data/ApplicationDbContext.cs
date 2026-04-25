using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AiChatSession> AiChatSessions { get; set; }

    public virtual DbSet<AiGenerationHistory> AiGenerationHistories { get; set; }

    public virtual DbSet<AiMessage> AiMessages { get; set; }

    public virtual DbSet<AiRiskAssessment> AiRiskAssessments { get; set; }

    public virtual DbSet<ApprovalAction> ApprovalActions { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Budget> Budgets { get; set; }

    public virtual DbSet<BudgetAdjustment> BudgetAdjustments { get; set; }

    public virtual DbSet<BudgetCategory> BudgetCategories { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeHistory> EmployeeHistories { get; set; }

    public virtual DbSet<EvaluationPeriod> EvaluationPeriods { get; set; }

    public virtual DbSet<FileUpload> FileUploads { get; set; }

    public virtual DbSet<FiscalPeriod> FiscalPeriods { get; set; }

    public virtual DbSet<KeyResult> KeyResults { get; set; }

    public virtual DbSet<Kpi> Kpis { get; set; }

    public virtual DbSet<KpiCheckIn> KpiCheckIns { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationPreference> NotificationPreferences { get; set; }

    public virtual DbSet<Objective> Objectives { get; set; }

    public virtual DbSet<PaymentRequest> PaymentRequests { get; set; }

    public virtual DbSet<PaymentRequestAttachment> PaymentRequestAttachments { get; set; }

    public virtual DbSet<PaymentRequestItem> PaymentRequestItems { get; set; }

    public virtual DbSet<PerformanceEvaluation> PerformanceEvaluations { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserLoginAttempt> UserLoginAttempts { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<Vendor> Vendors { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WorkflowCondition> WorkflowConditions { get; set; }

    public virtual DbSet<WorkflowInstance> WorkflowInstances { get; set; }

    public virtual DbSet<WorkflowInstanceStep> WorkflowInstanceSteps { get; set; }

    public virtual DbSet<WorkflowStep> WorkflowSteps { get; set; }

    public virtual DbSet<WorkflowTemplate> WorkflowTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("biz");

        modelBuilder.Entity<AiChatSession>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<AiGenerationHistory>(entity =>
        {
            entity.ToTable("AiGenerationHistory");

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<AiMessage>(entity =>
        {
            entity.HasIndex(e => e.SessionId, "IX_AiMessages_SessionId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Session).WithMany(p => p.AiMessages)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AiRiskAssessment>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RiskScore).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<ApprovalAction>(entity =>
        {
            entity.HasIndex(e => e.InstanceStepId, "IX_ApprovalActions_InstanceStepId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Action).HasMaxLength(20);

            entity.HasOne(d => d.InstanceStep).WithMany(p => p.ApprovalActions)
                .HasForeignKey(d => d.InstanceStepId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_Budgets_CategoryId");

            entity.HasIndex(e => e.CompanyId, "IX_Budgets_CompanyId");

            entity.HasIndex(e => new { e.DepartmentId, e.FiscalPeriodId }, "IX_Budgets_DepartmentId_FiscalPeriodId");

            entity.HasIndex(e => e.FiscalPeriodId, "IX_Budgets_FiscalPeriodId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AllocatedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CommittedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SpentAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.WarningThreshold).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Budgets)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Company).WithMany(p => p.Budgets)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Department).WithMany(p => p.Budgets)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.FiscalPeriod).WithMany(p => p.Budgets)
                .HasForeignKey(d => d.FiscalPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<BudgetAdjustment>(entity =>
        {
            entity.HasIndex(e => e.BudgetId, "IX_BudgetAdjustments_BudgetId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NewAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PreviousAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Budget).WithMany(p => p.BudgetAdjustments)
                .HasForeignKey(d => d.BudgetId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<BudgetCategory>(entity =>
        {
            entity.HasIndex(e => e.Code, "IX_BudgetCategories_Code").IsUnique();

            entity.HasIndex(e => e.CompanyId, "IX_BudgetCategories_CompanyId");

            entity.HasIndex(e => e.ParentId, "IX_BudgetCategories_ParentId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Type).HasMaxLength(20);

            entity.HasOne(d => d.Company).WithMany(p => p.BudgetCategories)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent).HasForeignKey(d => d.ParentId);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(e => e.TaxCode, "IX_Companies_TaxCode")
                .IsUnique()
                .HasFilter("([TaxCode] IS NOT NULL)");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(e => e.Code, "IX_Departments_Code").IsUnique();

            entity.HasIndex(e => e.CompanyId, "IX_Departments_CompanyId");

            entity.HasIndex(e => e.ManagerId, "IX_Departments_ManagerId");

            entity.HasIndex(e => e.ParentDepartmentId, "IX_Departments_ParentDepartmentId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BudgetLimit).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Company).WithMany(p => p.Departments)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Manager).WithMany(p => p.Departments).HasForeignKey(d => d.ManagerId);

            entity.HasOne(d => d.ParentDepartment).WithMany(p => p.InverseParentDepartment).HasForeignKey(d => d.ParentDepartmentId);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.CompanyId, "IX_Employees_CompanyId");

            entity.HasIndex(e => e.DepartmentId, "IX_Employees_DepartmentId");

            entity.HasIndex(e => e.Email, "IX_Employees_Email").IsUnique();

            entity.HasIndex(e => e.EmployeeCode, "IX_Employees_EmployeeCode").IsUnique();

            entity.HasIndex(e => e.ManagerId, "IX_Employees_ManagerId");

            entity.HasIndex(e => e.PositionId, "IX_Employees_PositionId");

            entity.HasIndex(e => e.UserId, "IX_Employees_UserId")
                .IsUnique()
                .HasFilter("([UserId] IS NOT NULL)");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Company).WithMany(p => p.Employees)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Department).WithMany(p => p.Employees).HasForeignKey(d => d.DepartmentId);

            entity.HasOne(d => d.Manager).WithMany(p => p.InverseManager).HasForeignKey(d => d.ManagerId);

            entity.HasOne(d => d.Position).WithMany(p => p.Employees).HasForeignKey(d => d.PositionId);

            entity.HasOne(d => d.User).WithOne(p => p.Employee).HasForeignKey<Employee>(d => d.UserId);
        });

        modelBuilder.Entity<EmployeeHistory>(entity =>
        {
            entity.ToTable("EmployeeHistory");

            entity.HasIndex(e => e.EmployeeId, "IX_EmployeeHistory_EmployeeId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeHistories)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<EvaluationPeriod>(entity =>
        {
            entity.HasIndex(e => e.CompanyId, "IX_EvaluationPeriods_CompanyId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.KpiWeight).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OkrWeight).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Company).WithMany(p => p.EvaluationPeriods)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<FileUpload>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<FiscalPeriod>(entity =>
        {
            entity.HasIndex(e => e.CompanyId, "IX_FiscalPeriods_CompanyId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Company).WithMany(p => p.FiscalPeriods)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<KeyResult>(entity =>
        {
            entity.HasIndex(e => e.AssigneeId, "IX_KeyResults_AssigneeId");

            entity.HasIndex(e => e.ObjectiveId, "IX_KeyResults_ObjectiveId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CurrentValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Direction).HasMaxLength(20);
            entity.Property(e => e.MetricType).HasMaxLength(20);
            entity.Property(e => e.Progress).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.StartValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TargetValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Assignee).WithMany(p => p.KeyResults).HasForeignKey(d => d.AssigneeId);

            entity.HasOne(d => d.Objective).WithMany(p => p.KeyResults)
                .HasForeignKey(d => d.ObjectiveId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Kpi>(entity =>
        {
            entity.HasIndex(e => e.AssigneeId, "IX_Kpis_AssigneeId");

            entity.HasIndex(e => e.CompanyId, "IX_Kpis_CompanyId");

            entity.HasIndex(e => e.DepartmentId, "IX_Kpis_DepartmentId");

            entity.HasIndex(e => e.PeriodId, "IX_Kpis_PeriodId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CurrentValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Direction).HasMaxLength(20);
            entity.Property(e => e.MetricType).HasMaxLength(20);
            entity.Property(e => e.Progress).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Score).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StartValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TargetValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Assignee).WithMany(p => p.Kpis).HasForeignKey(d => d.AssigneeId);

            entity.HasOne(d => d.Company).WithMany(p => p.Kpis)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Department).WithMany(p => p.Kpis).HasForeignKey(d => d.DepartmentId);

            entity.HasOne(d => d.Period).WithMany(p => p.Kpis)
                .HasForeignKey(d => d.PeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<KpiCheckIn>(entity =>
        {
            entity.HasIndex(e => e.KpiId, "IX_KpiCheckIns_KpiId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.NewValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PreviousValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Progress).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Kpi).WithMany(p => p.KpiCheckIns)
                .HasForeignKey(d => d.KpiId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Objective>(entity =>
        {
            entity.HasIndex(e => e.CompanyId, "IX_Objectives_CompanyId");

            entity.HasIndex(e => e.DepartmentId, "IX_Objectives_DepartmentId");

            entity.HasIndex(e => e.OwnerId, "IX_Objectives_OwnerId");

            entity.HasIndex(e => e.ParentId, "IX_Objectives_ParentId");

            entity.HasIndex(e => e.PeriodId, "IX_Objectives_PeriodId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.OwnerType).HasMaxLength(20);
            entity.Property(e => e.Progress).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Company).WithMany(p => p.Objectives)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Department).WithMany(p => p.Objectives).HasForeignKey(d => d.DepartmentId);

            entity.HasOne(d => d.Owner).WithMany(p => p.Objectives).HasForeignKey(d => d.OwnerId);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent).HasForeignKey(d => d.ParentId);

            entity.HasOne(d => d.Period).WithMany(p => p.Objectives)
                .HasForeignKey(d => d.PeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PaymentRequest>(entity =>
        {
            entity.HasIndex(e => e.BudgetId, "IX_PaymentRequests_BudgetId");

            entity.HasIndex(e => e.CategoryId, "IX_PaymentRequests_CategoryId");

            entity.HasIndex(e => e.CompanyId, "IX_PaymentRequests_CompanyId");

            entity.HasIndex(e => e.DepartmentId, "IX_PaymentRequests_DepartmentId");

            entity.HasIndex(e => e.RequestNumber, "IX_PaymentRequests_RequestNumber").IsUnique();

            entity.HasIndex(e => e.RequesterId, "IX_PaymentRequests_RequesterId");

            entity.HasIndex(e => e.VendorId, "IX_PaymentRequests_VendorId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AiRiskScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Budget).WithMany(p => p.PaymentRequests).HasForeignKey(d => d.BudgetId);

            entity.HasOne(d => d.Category).WithMany(p => p.PaymentRequests)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Company).WithMany(p => p.PaymentRequests)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Department).WithMany(p => p.PaymentRequests)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Requester).WithMany(p => p.PaymentRequests)
                .HasForeignKey(d => d.RequesterId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Vendor).WithMany(p => p.PaymentRequests).HasForeignKey(d => d.VendorId);
        });

        modelBuilder.Entity<PaymentRequestAttachment>(entity =>
        {
            entity.HasIndex(e => e.PaymentRequestId, "IX_PaymentRequestAttachments_PaymentRequestId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.PaymentRequest).WithMany(p => p.PaymentRequestAttachments)
                .HasForeignKey(d => d.PaymentRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PaymentRequestItem>(entity =>
        {
            entity.HasIndex(e => e.PaymentRequestId, "IX_PaymentRequestItems_PaymentRequestId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Quantity).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxRate).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.PaymentRequest).WithMany(p => p.PaymentRequestItems)
                .HasForeignKey(d => d.PaymentRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PerformanceEvaluation>(entity =>
        {
            entity.HasIndex(e => e.EmployeeId, "IX_PerformanceEvaluations_EmployeeId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.KpiScore).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OkrScore).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalScore).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Employee).WithMany(p => p.PerformanceEvaluations)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => new { e.Module, e.Action, e.Resource }, "IX_Permissions_Module_Action_Resource").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.Module).HasMaxLength(50);
            entity.Property(e => e.Resource).HasMaxLength(100);
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasIndex(e => e.CompanyId, "IX_Positions_CompanyId");

            entity.HasIndex(e => e.DepartmentId, "IX_Positions_DepartmentId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Company).WithMany(p => p.Positions)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Department).WithMany(p => p.Positions).HasForeignKey(d => d.DepartmentId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token, "IX_RefreshTokens_Token").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_RefreshTokens_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_Roles_Name").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(50);

        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasIndex(e => e.BudgetId, "IX_Transactions_BudgetId");

            entity.HasIndex(e => e.CategoryId, "IX_Transactions_CategoryId");

            entity.HasIndex(e => e.CompanyId, "IX_Transactions_CompanyId");

            entity.HasIndex(e => e.DepartmentId, "IX_Transactions_DepartmentId");

            entity.HasIndex(e => e.PaymentRequestId, "IX_Transactions_PaymentRequestId");

            entity.HasIndex(e => e.TransactionNumber, "IX_Transactions_TransactionNumber").IsUnique();

            entity.HasIndex(e => e.VendorId, "IX_Transactions_VendorId");

            entity.HasIndex(e => e.WalletId, "IX_Transactions_WalletId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Type).HasMaxLength(20);

            entity.HasOne(d => d.Budget).WithMany(p => p.Transactions).HasForeignKey(d => d.BudgetId);

            entity.HasOne(d => d.Category).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Company).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Department).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.PaymentRequest).WithMany(p => p.Transactions).HasForeignKey(d => d.PaymentRequestId);

            entity.HasOne(d => d.Vendor).WithMany(p => p.Transactions).HasForeignKey(d => d.VendorId);

            entity.HasOne(d => d.Wallet).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.HasIndex(e => e.RoleId, "IX_UserRoles_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserSessions_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.HasIndex(e => e.CompanyId, "IX_Vendors_CompanyId");

            entity.HasIndex(e => e.TaxCode, "IX_Vendors_TaxCode")
                .IsUnique()
                .HasFilter("([TaxCode] IS NOT NULL)");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)");

            entity.HasOne(d => d.Company).WithMany(p => p.Vendors)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasIndex(e => e.CompanyId, "IX_Wallets_CompanyId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Balance).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Company).WithMany(p => p.Wallets)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WorkflowCondition>(entity =>
        {
            entity.HasIndex(e => e.TemplateId, "IX_WorkflowConditions_TemplateId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Template).WithMany(p => p.WorkflowConditions)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "IX_WorkflowInstances_EntityType_EntityId");

            entity.HasIndex(e => e.TemplateId, "IX_WorkflowInstances_TemplateId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Template).WithMany(p => p.WorkflowInstances)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WorkflowInstanceStep>(entity =>
        {
            entity.HasIndex(e => e.InstanceId, "IX_WorkflowInstanceSteps_InstanceId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Instance).WithMany(p => p.WorkflowInstanceSteps)
                .HasForeignKey(d => d.InstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WorkflowStep>(entity =>
        {
            entity.HasIndex(e => e.TemplateId, "IX_WorkflowSteps_TemplateId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Template).WithMany(p => p.WorkflowSteps)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WorkflowTemplate>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
        ApplyCodeFirstConventions(modelBuilder);
    }

    private static void ApplyCodeFirstConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.FindProperty("Id")?.ClrType == typeof(Guid))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasDefaultValueSql("NEWID()");
            }
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
