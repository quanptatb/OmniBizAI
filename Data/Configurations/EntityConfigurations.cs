using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.BusinessType).HasMaxLength(100);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.Property(e => e.Status).HasConversion<int>();
    }
}

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(255).IsRequired();
        builder.Property(e => e.JobTitle).HasMaxLength(150);
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasOne(e => e.OrganizationUnit)
            .WithMany()
            .HasForeignKey(e => e.OrganizationUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();

        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OperationRequestConfiguration : IEntityTypeConfiguration<OperationRequest>
{
    public void Configure(EntityTypeBuilder<OperationRequest> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.RequestNo }).IsUnique();
        builder.Property(e => e.RequestNo).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Priority).HasConversion<int>();
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

        builder.HasOne(e => e.RequestedByUser)
            .WithMany()
            .HasForeignKey(e => e.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class ApprovalTaskConfiguration : IEntityTypeConfiguration<ApprovalTask>
{
    public void Configure(EntityTypeBuilder<ApprovalTask> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.TargetType, e.TargetId });
        builder.Property(e => e.TargetType).HasMaxLength(80).IsRequired();
        builder.Property(e => e.StepCode).HasMaxLength(80).IsRequired();
        builder.Property(e => e.AssignedRole).HasMaxLength(80);
        builder.Property(e => e.DecisionNote).HasMaxLength(1000);
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Status, e.DueDate });
        builder.HasIndex(e => new { e.TenantId, e.KanbanColumnId });
        builder.Property(e => e.Title).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.Priority).HasConversion<int>();

        builder.HasOne(e => e.KanbanColumn)
            .WithMany(c => c.WorkItems)
            .HasForeignKey(e => e.KanbanColumnId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class KanbanColumnConfiguration : IEntityTypeConfiguration<KanbanColumn>
{
    public void Configure(EntityTypeBuilder<KanbanColumn> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.SortOrder });
        builder.Property(e => e.Title).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.AccentColor).HasMaxLength(50);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserName).HasMaxLength(200);
        builder.Property(e => e.EntityName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(100).IsRequired();
        builder.Property(e => e.OldValuesJson).HasMaxLength(4000);
        builder.Property(e => e.NewValuesJson).HasMaxLength(4000);
        builder.Property(e => e.IpAddress).HasMaxLength(100);
    }
}

public class AiInsightConfiguration : IEntityTypeConfiguration<AiInsight>
{
    public void Configure(EntityTypeBuilder<AiInsight> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ContextType).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Question).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Recommendation).HasMaxLength(4000);
        builder.Property(e => e.RiskLevel).HasConversion<int>();
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class KpiDefinitionConfiguration : IEntityTypeConfiguration<KpiDefinition>
{
    public void Configure(EntityTypeBuilder<KpiDefinition> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Unit).HasMaxLength(50);
        builder.Property(e => e.OwnerType).HasConversion<int>();
        builder.Property(e => e.PeriodType).HasConversion<int>();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.PlannedAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class PaymentRequestConfiguration : IEntityTypeConfiguration<PaymentRequest>
{
    public void Configure(EntityTypeBuilder<PaymentRequest> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RequestNo).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(2000);
        builder.Property(e => e.Status).HasConversion<int>();
    }
}

public class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UserId, e.RoleDefinitionId, e.TenantId }).IsUnique();
    }
}

public class ProcurementRequestConfiguration : IEntityTypeConfiguration<ProcurementRequest>
{
    public void Configure(EntityTypeBuilder<ProcurementRequest> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RequestNo).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
