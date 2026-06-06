using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;

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

        builder.HasOne(e => e.ManagerUser)
            .WithMany()
            .HasForeignKey(e => e.ManagerUserId)
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
        builder.Property(e => e.Priority).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualCost).HasColumnType("decimal(18,2)");
        builder.Property(e => e.CostVariance).HasColumnType("decimal(18,2)");
        builder.Property(e => e.CostVariancePercent).HasColumnType("decimal(9,2)");
        builder.HasIndex(e => new { e.TenantId, e.CostVariancePercent });

        builder.HasOne(e => e.RequestedByUser)
            .WithMany()
            .HasForeignKey(e => e.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.KpiDefinition)
            .WithOne(k => k.OperationRequest)
            .HasForeignKey<KpiDefinition>(k => k.OperationRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OkrObjective)
            .WithOne(o => o.OperationRequest)
            .HasForeignKey<OkrObjective>(o => o.OperationRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntityName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(260).IsRequired();
        builder.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(100);
        builder.HasIndex(e => new { e.TenantId, e.EntityName, e.EntityId });

        builder.HasOne(e => e.UploadedByUser)
            .WithMany()
            .HasForeignKey(e => e.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OperationRequestTemplateConfiguration : IEntityTypeConfiguration<OperationRequestTemplate>
{
    public void Configure(EntityTypeBuilder<OperationRequestTemplate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Priority).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.HasIndex(e => new { e.TenantId, e.IsActive, e.Type });

        builder.HasOne(e => e.DefaultDepartment)
            .WithMany()
            .HasForeignKey(e => e.DefaultDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OperationRequestAssignmentConfiguration : IEntityTypeConfiguration<OperationRequestAssignment>
{
    public void Configure(EntityTypeBuilder<OperationRequestAssignment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_OperationRequestAssignments_Target",
            "([AssignedUserId] IS NOT NULL AND [OrganizationUnitId] IS NULL) OR ([AssignedUserId] IS NULL AND [OrganizationUnitId] IS NOT NULL)"));
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.HasIndex(e => new { e.TenantId, e.OperationRequestId, e.Role, e.IsActive });

        builder.HasOne(e => e.OperationRequest)
            .WithMany(e => e.Assignments)
            .HasForeignKey(e => e.OperationRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedUser)
            .WithMany()
            .HasForeignKey(e => e.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OrganizationUnit)
            .WithMany()
            .HasForeignKey(e => e.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OperationCommentConfiguration : IEntityTypeConfiguration<OperationComment>
{
    public void Configure(EntityTypeBuilder<OperationComment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Content).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(e => new { e.TenantId, e.OperationRequestId, e.CreatedAt });
        builder.HasIndex(e => e.ParentCommentId);

        builder.HasOne(e => e.OperationRequest)
            .WithMany(e => e.Comments)
            .HasForeignKey(e => e.OperationRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AuthorUser)
            .WithMany()
            .HasForeignKey(e => e.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ParentComment)
            .WithMany(e => e.Replies)
            .HasForeignKey(e => e.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OperationProgressLogConfiguration : IEntityTypeConfiguration<OperationProgressLog>
{
    public void Configure(EntityTypeBuilder<OperationProgressLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ProgressPercent).HasColumnType("decimal(5,2)");
        builder.Property(e => e.Note).HasMaxLength(1000);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.OperationRequestId, e.CreatedAt });

        builder.HasOne(e => e.OperationRequest)
            .WithMany(e => e.ProgressLogs)
            .HasForeignKey(e => e.OperationRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OperationSlaPolicyConfiguration : IEntityTypeConfiguration<OperationSlaPolicy>
{
    public void Configure(EntityTypeBuilder<OperationSlaPolicy> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Priority).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(e => new { e.TenantId, e.Priority })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OperationSlaBreachConfiguration : IEntityTypeConfiguration<OperationSlaBreach>
{
    public void Configure(EntityTypeBuilder<OperationSlaBreach> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.BreachType).HasConversion<string>().HasMaxLength(40);
        builder.Property(e => e.HoursOverdue).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.HasIndex(e => new { e.TenantId, e.OperationRequestId, e.BreachType })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(e => e.OperationRequest)
            .WithMany(e => e.SlaBreaches)
            .HasForeignKey(e => e.OperationRequestId)
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
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.RowVersion).IsRowVersion();

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
        builder.HasIndex(e => new { e.TenantId, e.SprintId, e.Status });
        builder.Property(e => e.Title).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Priority).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(e => e.KanbanColumn)
            .WithMany(c => c.WorkItems)
            .HasForeignKey(e => e.KanbanColumnId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Sprint)
            .WithMany(s => s.WorkItems)
            .HasForeignKey(e => e.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Status, e.StartDate });
        builder.Property(e => e.Name).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Goal).HasMaxLength(1000);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Sprints_DateRange",
            "[EndDate] >= [StartDate]"));

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
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_KanbanColumns_WipLimit",
            "[WipLimit] IS NULL OR [WipLimit] > 0"));

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class KanbanSavedViewConfiguration : IEntityTypeConfiguration<KanbanSavedView>
{
    public void Configure(EntityTypeBuilder<KanbanSavedView> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.UserId, e.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.Property(e => e.Name).HasMaxLength(120).IsRequired();
        builder.Property(e => e.SearchTerm).HasMaxLength(200);
        builder.Property(e => e.Priority).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.QuickFilter).HasMaxLength(40);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkItemActivityConfiguration : IEntityTypeConfiguration<WorkItemActivity>
{
    public void Configure(EntityTypeBuilder<WorkItemActivity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.WorkItemId, e.MovedAt });
        builder.HasIndex(e => new { e.TenantId, e.ToColumnId, e.MovedAt });

        builder.HasOne(e => e.WorkItem)
            .WithMany()
            .HasForeignKey(e => e.WorkItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.FromColumn)
            .WithMany()
            .HasForeignKey(e => e.FromColumnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ToColumn)
            .WithMany()
            .HasForeignKey(e => e.ToColumnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.MovedByUser)
            .WithMany()
            .HasForeignKey(e => e.MovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkItemDependencyConfiguration : IEntityTypeConfiguration<WorkItemDependency>
{
    public void Configure(EntityTypeBuilder<WorkItemDependency> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.BlockedId, e.Type });
        builder.HasIndex(e => new { e.TenantId, e.BlockerId, e.Type });
        builder.HasIndex(e => new { e.TenantId, e.BlockerId, e.BlockedId, e.Type })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_WorkItemDependencies_NoSelfDependency",
            "[BlockerId] <> [BlockedId]"));

        builder.HasOne(e => e.Blocker)
            .WithMany()
            .HasForeignKey(e => e.BlockerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Blocked)
            .WithMany()
            .HasForeignKey(e => e.BlockedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkItemChecklistConfiguration : IEntityTypeConfiguration<WorkItemChecklist>
{
    public void Configure(EntityTypeBuilder<WorkItemChecklist> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.WorkItemId, e.SortOrder });
        builder.HasIndex(e => new { e.TenantId, e.AssignedToUserId, e.DueDate });
        builder.Property(e => e.Title).HasMaxLength(250).IsRequired();

        builder.HasOne(e => e.WorkItem)
            .WithMany(w => w.Checklists)
            .HasForeignKey(e => e.WorkItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedToUser)
            .WithMany()
            .HasForeignKey(e => e.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CompletedByUser)
            .WithMany()
            .HasForeignKey(e => e.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OperationPlanConfiguration : IEntityTypeConfiguration<OperationPlan>
{
    public void Configure(EntityTypeBuilder<OperationPlan> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.PlanType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.Status, e.ProjectedEndDate });
        builder.HasIndex(e => new { e.TenantId, e.SourceOperationRequestId })
            .IsUnique()
            .HasFilter("[SourceOperationRequestId] IS NOT NULL");
        builder.HasOne(e => e.SourceOperationRequest)
            .WithMany(e => e.OperationPlans)
            .HasForeignKey(e => e.SourceOperationRequestId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class PlanTaskConfiguration : IEntityTypeConfiguration<PlanTask>
{
    public void Configure(EntityTypeBuilder<PlanTask> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.UnitsProduced).HasColumnType("decimal(18,2)");
        builder.Property(e => e.UnitsGood).HasColumnType("decimal(18,2)");
        builder.Property(e => e.OeeAvailabilityPercent).HasColumnType("decimal(9,2)");
        builder.Property(e => e.OeePerformancePercent).HasColumnType("decimal(9,2)");
        builder.Property(e => e.OeeQualityPercent).HasColumnType("decimal(9,2)");
        builder.Property(e => e.OeePercent).HasColumnType("decimal(9,2)");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.PlanId, e.IsCriticalPath });
        builder.HasIndex(e => new { e.TenantId, e.PlanId, e.EarlyStart });
        builder.HasIndex(e => new { e.TenantId, e.EquipmentId, e.Status, e.ActualEndTime });

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class PlanTaskDependencyConfiguration : IEntityTypeConfiguration<PlanTaskDependency>
{
    public void Configure(EntityTypeBuilder<PlanTaskDependency> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(40);
        builder.HasIndex(e => new { e.TenantId, e.PlanId });
        builder.HasIndex(e => new { e.TenantId, e.PredecessorTaskId, e.SuccessorTaskId, e.Type })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PlanTaskDependencies_NoSelfDependency",
            "[PredecessorTaskId] <> [SuccessorTaskId]"));

        builder.HasOne(e => e.Plan)
            .WithMany(e => e.TaskDependencies)
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PredecessorTask)
            .WithMany(e => e.SuccessorDependencies)
            .HasForeignKey(e => e.PredecessorTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SuccessorTask)
            .WithMany(e => e.PredecessorDependencies)
            .HasForeignKey(e => e.SuccessorTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class PlanTaskBaselineConfiguration : IEntityTypeConfiguration<PlanTaskBaseline>
{
    public void Configure(EntityTypeBuilder<PlanTaskBaseline> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TaskName).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.PlanTaskId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => new { e.TenantId, e.PlanId });

        builder.HasOne(e => e.Plan)
            .WithMany(e => e.TaskBaselines)
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PlanTask)
            .WithMany()
            .HasForeignKey(e => e.PlanTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.BaselineAssignedUser)
            .WithMany()
            .HasForeignKey(e => e.BaselineAssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.BaselineEquipment)
            .WithMany()
            .HasForeignKey(e => e.BaselineEquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SnapshottedByUser)
            .WithMany()
            .HasForeignKey(e => e.SnapshottedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class PlanChangeOrderConfiguration : IEntityTypeConfiguration<PlanChangeOrder>
{
    public void Configure(EntityTypeBuilder<PlanChangeOrder> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(e => new { e.TenantId, e.PlanId, e.CreatedAt });
        builder.HasIndex(e => new { e.TenantId, e.PlanTaskId, e.CreatedAt });

        builder.HasOne(e => e.Plan)
            .WithMany(e => e.ChangeOrders)
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PlanTask)
            .WithMany(e => e.ChangeOrders)
            .HasForeignKey(e => e.PlanTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OldAssignedUser)
            .WithMany()
            .HasForeignKey(e => e.OldAssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NewAssignedUser)
            .WithMany()
            .HasForeignKey(e => e.NewAssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OldEquipment)
            .WithMany()
            .HasForeignKey(e => e.OldEquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NewEquipment)
            .WithMany()
            .HasForeignKey(e => e.NewEquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class GoodsIssueConfiguration : IEntityTypeConfiguration<GoodsIssue>
{
    public void Configure(EntityTypeBuilder<GoodsIssue> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.IssueNo).HasMaxLength(50).IsRequired();
        builder.Property(e => e.IssueType).HasMaxLength(50);
        builder.HasIndex(e => new { e.TenantId, e.OperationRequestId, e.Status });

        builder.HasOne(e => e.OperationRequest)
            .WithMany(e => e.GoodsIssues)
            .HasForeignKey(e => e.OperationRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class GoodsIssueLineConfiguration : IEntityTypeConfiguration<GoodsIssueLine>
{
    public void Configure(EntityTypeBuilder<GoodsIssueLine> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RequestedQuantity).HasColumnType("decimal(18,2)");
        builder.Property(e => e.IssuedQuantity).HasColumnType("decimal(18,2)");
        builder.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(e => e.LineAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.ItemName).HasMaxLength(250);
        builder.Property(e => e.UnitOfMeasure).HasMaxLength(50);
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.HasIndex(e => new { e.TenantId, e.GoodsIssueId });

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(100);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Location).HasMaxLength(200);
        builder.Property(e => e.Manufacturer).HasMaxLength(100);
        builder.Property(e => e.Model).HasMaxLength(100);
        builder.Property(e => e.SerialNumber).HasMaxLength(100);
        builder.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class EquipmentStatusHistoryConfiguration : IEntityTypeConfiguration<EquipmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<EquipmentStatusHistory> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OldStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.NewStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.HasIndex(e => new { e.TenantId, e.EquipmentId, e.ChangedAt });
        builder.HasOne(e => e.Equipment)
            .WithMany(e => e.StatusHistories)
            .HasForeignKey(e => e.EquipmentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class EquipmentCostLedgerConfiguration : IEntityTypeConfiguration<EquipmentCostLedger>
{
    public void Configure(EntityTypeBuilder<EquipmentCostLedger> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CostType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.SourceType).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.HasIndex(e => new { e.TenantId, e.EquipmentId, e.OccurredDate });
        builder.HasOne(e => e.Equipment)
            .WithMany(e => e.CostLedgers)
            .HasForeignKey(e => e.EquipmentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.MaintenanceType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Cost).HasColumnType("decimal(18,2)");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50);
        builder.Property(e => e.Location).HasMaxLength(200);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class PmScheduleConfiguration : IEntityTypeConfiguration<PmSchedule>
{
    public void Configure(EntityTypeBuilder<PmSchedule> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TaskName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Frequency).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.TriggerType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.ConditionSensorType).HasMaxLength(100);
        builder.Property(e => e.Checklist).HasMaxLength(200);
        builder.HasIndex(e => new { e.TenantId, e.EquipmentId, e.NextDueDate });

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class MaintenanceIncidentConfiguration : IEntityTypeConfiguration<MaintenanceIncident>
{
    public void Configure(EntityTypeBuilder<MaintenanceIncident> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Severity).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.DowntimeHours).HasColumnType("decimal(18,2)");
        builder.Property(e => e.FiveWhysJson).HasMaxLength(4000);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => new { e.TenantId, e.EquipmentId, e.Status });

        builder.HasOne(e => e.FailureMode)
            .WithMany(m => m.Incidents)
            .HasForeignKey(e => e.FailureModeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class EquipmentSensorReadingConfiguration : IEntityTypeConfiguration<EquipmentSensorReading>
{
    public void Configure(EntityTypeBuilder<EquipmentSensorReading> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SensorType).HasMaxLength(100);
        builder.Property(e => e.Unit).HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);

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
        builder.Property(e => e.ExtraJson).HasMaxLength(4000);
        builder.Property(e => e.IpAddress).HasMaxLength(100);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
    }
}

public class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Code, e.Year })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.Property(e => e.Code).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Prefix).HasMaxLength(30).IsRequired();
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

// KpiDefinition configuration moved to KpiOkrConfigurations.cs


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
        builder.HasIndex(e => new { e.TenantId, e.OperationRequestId });

        builder.HasOne(e => e.OperationRequest)
            .WithMany(e => e.PaymentRequests)
            .HasForeignKey(e => e.OperationRequestId)
            .OnDelete(DeleteBehavior.Restrict);

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

// ── Maintenance Module (F5.x) ────────────────────────────────────────────────

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Status, e.ScheduledStart });
        builder.HasIndex(e => new { e.TenantId, e.EquipmentId });
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Priority).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.EstimatedHours).HasColumnType("decimal(9,2)");
        builder.Property(e => e.ActualHours).HasColumnType("decimal(9,2)");

        builder.HasOne(e => e.Equipment).WithMany().HasForeignKey(e => e.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.RequestedByUser).WithMany().HasForeignKey(e => e.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.AssignedTechnician).WithMany().HasForeignKey(e => e.AssignedTechnicianId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.CompletedByUser).WithMany().HasForeignKey(e => e.CompletedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Incident).WithMany().HasForeignKey(e => e.IncidentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.PmSchedule).WithMany().HasForeignKey(e => e.PmScheduleId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkOrderChecklistItemConfiguration : IEntityTypeConfiguration<WorkOrderChecklistItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderChecklistItem> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.WorkOrderId, e.SortOrder });
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();

        builder.HasOne(e => e.WorkOrder).WithMany(w => w.ChecklistItems).HasForeignKey(e => e.WorkOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.CompletedByUser).WithMany().HasForeignKey(e => e.CompletedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkOrderSparePartUsageConfiguration : IEntityTypeConfiguration<WorkOrderSparePartUsage>
{
    public void Configure(EntityTypeBuilder<WorkOrderSparePartUsage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.WorkOrderId });
        builder.HasIndex(e => new { e.TenantId, e.SparePartId });

        builder.HasOne(e => e.WorkOrder).WithMany(w => w.PartUsages).HasForeignKey(e => e.WorkOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.SparePart).WithMany(s => s.WorkOrderUsages).HasForeignKey(e => e.SparePartId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class SparePartRequisitionConfiguration : IEntityTypeConfiguration<SparePartRequisition>
{
    public void Configure(EntityTypeBuilder<SparePartRequisition> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(e => e.RequestedByUser).WithMany().HasForeignKey(e => e.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ApprovedByUser).WithMany().HasForeignKey(e => e.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.LinkedWorkOrder).WithMany().HasForeignKey(e => e.LinkedWorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.IssuedGoodsIssue).WithMany().HasForeignKey(e => e.IssuedGoodsIssueId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class SparePartRequisitionLineConfiguration : IEntityTypeConfiguration<SparePartRequisitionLine>
{
    public void Configure(EntityTypeBuilder<SparePartRequisitionLine> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.RequisitionId });

        builder.HasOne(e => e.Requisition).WithMany(r => r.Lines).HasForeignKey(e => e.RequisitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.SparePart).WithMany(s => s.RequisitionLines).HasForeignKey(e => e.SparePartId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class FailureModeConfiguration : IEntityTypeConfiguration<FailureMode>
{
    public void Configure(EntityTypeBuilder<FailureMode> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Category });
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(30);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
