using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Data.Configurations;

// ── Mission / Vision ─────────────────────────────────────────────────────────
public class MissionVisionConfiguration : IEntityTypeConfiguration<MissionVision>
{
    public void Configure(EntityTypeBuilder<MissionVision> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Content).HasMaxLength(4000);
        b.Property(e => e.FinancialTarget).HasColumnType("decimal(18,2)");
        b.Property(e => e.Type).HasConversion<int>();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── OKR Objective ────────────────────────────────────────────────────────────
public class OkrObjectiveConfiguration : IEntityTypeConfiguration<OkrObjective>
{
    public void Configure(EntityTypeBuilder<OkrObjective> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.ObjectiveName).HasMaxLength(255).IsRequired();
        b.Property(e => e.Cycle).HasMaxLength(50);
        b.Property(e => e.Level).HasConversion<int>();
        b.Property(e => e.Status).HasConversion<int>();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── OKR Key Result ───────────────────────────────────────────────────────────
public class OkrKeyResultConfiguration : IEntityTypeConfiguration<OkrKeyResult>
{
    public void Configure(EntityTypeBuilder<OkrKeyResult> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.KeyResultName).HasMaxLength(500).IsRequired();
        b.Property(e => e.Unit).HasMaxLength(50);
        b.Property(e => e.TargetValue).HasColumnType("decimal(18,2)");
        b.Property(e => e.CurrentValue).HasColumnType("decimal(18,2)");

        b.HasOne(e => e.OkrObjective).WithMany(o => o.KeyResults)
            .HasForeignKey(e => e.OkrObjectiveId).OnDelete(DeleteBehavior.Cascade);

        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── OKR Join Tables ──────────────────────────────────────────────────────────
public class OkrMissionMappingConfiguration : IEntityTypeConfiguration<OkrMissionMapping>
{
    public void Configure(EntityTypeBuilder<OkrMissionMapping> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.OkrObjectiveId, e.MissionVisionId }).IsUnique();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OkrDepartmentAllocationConfiguration : IEntityTypeConfiguration<OkrDepartmentAllocation>
{
    public void Configure(EntityTypeBuilder<OkrDepartmentAllocation> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.OkrObjectiveId, e.OrganizationUnitId }).IsUnique();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class OkrEmployeeAllocationConfiguration : IEntityTypeConfiguration<OkrEmployeeAllocation>
{
    public void Configure(EntityTypeBuilder<OkrEmployeeAllocation> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.OkrObjectiveId, e.UserId }).IsUnique();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── Evaluation Period ────────────────────────────────────────────────────────
public class EvaluationPeriodConfiguration : IEntityTypeConfiguration<EvaluationPeriod>
{
    public void Configure(EntityTypeBuilder<EvaluationPeriod> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.PeriodName).HasMaxLength(100).IsRequired();
        b.Property(e => e.Description).HasMaxLength(500);
        b.Property(e => e.Status).HasConversion<int>();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── Grading Rank ─────────────────────────────────────────────────────────────
public class GradingRankConfiguration : IEntityTypeConfiguration<GradingRank>
{
    public void Configure(EntityTypeBuilder<GradingRank> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.RankName).HasMaxLength(50).IsRequired();
        b.Property(e => e.RankCode).HasConversion<int>();
        b.Property(e => e.MinScore).HasColumnType("decimal(5,2)");
        b.Property(e => e.MaxScore).HasColumnType("decimal(5,2)");
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── Bonus Rule ───────────────────────────────────────────────────────────────
public class BonusRuleConfiguration : IEntityTypeConfiguration<BonusRule>
{
    public void Configure(EntityTypeBuilder<BonusRule> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.SalaryPercentage).HasColumnType("decimal(5,2)");
        b.Property(e => e.FixedAmount).HasColumnType("decimal(18,2)");
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── KPI Definition (expanded) ────────────────────────────────────────────────
public class KpiDefinitionExpandedConfiguration : IEntityTypeConfiguration<KpiDefinition>
{
    public void Configure(EntityTypeBuilder<KpiDefinition> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Code).HasMaxLength(80).IsRequired();
        b.Property(e => e.Name).HasMaxLength(250).IsRequired();
        b.Property(e => e.Description).HasMaxLength(1000);
        b.Property(e => e.Unit).HasMaxLength(50);
        b.Property(e => e.OwnerType).HasConversion<int>();
        b.Property(e => e.PeriodType).HasConversion<int>();
        b.Property(e => e.MeasureType).HasConversion<int>();
        b.Property(e => e.PropertyType).HasConversion<int>();
        b.Property(e => e.Status).HasConversion<int>();

        b.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── KPI Target (expanded) ───────────────────────────────────────────────────
public class KpiTargetExpandedConfiguration : IEntityTypeConfiguration<KpiTarget>
{
    public void Configure(EntityTypeBuilder<KpiTarget> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.TargetValue).HasColumnType("decimal(18,2)");
        b.Property(e => e.PassThreshold).HasColumnType("decimal(18,2)");
        b.Property(e => e.FailThreshold).HasColumnType("decimal(18,2)");
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── KPI Check-In (expanded) ─────────────────────────────────────────────────
public class KpiCheckInExpandedConfiguration : IEntityTypeConfiguration<KpiCheckIn>
{
    public void Configure(EntityTypeBuilder<KpiCheckIn> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.ProgressValue).HasColumnType("decimal(18,2)");
        b.Property(e => e.Comment).HasMaxLength(1000);
        b.Property(e => e.ReviewStatus).HasConversion<int>();
        b.Property(e => e.ReviewComment).HasMaxLength(2000);
        b.Property(e => e.ReviewScore).HasColumnType("decimal(5,2)");
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── KPI Result (expanded) ───────────────────────────────────────────────────
public class KpiResultExpandedConfiguration : IEntityTypeConfiguration<KpiResult>
{
    public void Configure(EntityTypeBuilder<KpiResult> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.ActualValue).HasColumnType("decimal(18,2)");
        b.Property(e => e.Score).HasColumnType("decimal(18,2)");
        b.Property(e => e.ProgressPercent).HasColumnType("decimal(5,2)");
        b.Property(e => e.Classification).HasMaxLength(50);
        b.Property(e => e.Note).HasMaxLength(1000);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── KPI Department / Employee Assignments ────────────────────────────────────
public class KpiDepartmentAssignmentConfiguration : IEntityTypeConfiguration<KpiDepartmentAssignment>
{
    public void Configure(EntityTypeBuilder<KpiDepartmentAssignment> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.KpiDefinitionId, e.OrganizationUnitId }).IsUnique();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class KpiEmployeeAssignmentConfiguration : IEntityTypeConfiguration<KpiEmployeeAssignment>
{
    public void Configure(EntityTypeBuilder<KpiEmployeeAssignment> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.KpiDefinitionId, e.UserId }).IsUnique();
        b.Property(e => e.Weight).HasColumnType("decimal(5,2)");
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── KPI Check-In Detail & History ────────────────────────────────────────────
public class KpiCheckInDetailConfiguration : IEntityTypeConfiguration<KpiCheckInDetail>
{
    public void Configure(EntityTypeBuilder<KpiCheckInDetail> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.MetricName).HasMaxLength(255);
        b.Property(e => e.TargetValue).HasColumnType("decimal(18,2)");
        b.Property(e => e.AchievedValue).HasColumnType("decimal(18,2)");
        b.Property(e => e.Note).HasMaxLength(2000);

        b.HasOne(e => e.KpiCheckIn).WithMany(c => c.Details)
            .HasForeignKey(e => e.KpiCheckInId).OnDelete(DeleteBehavior.Cascade);

        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class KpiCheckInHistoryLogConfiguration : IEntityTypeConfiguration<KpiCheckInHistoryLog>
{
    public void Configure(EntityTypeBuilder<KpiCheckInHistoryLog> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Action).HasMaxLength(100).IsRequired();
        b.Property(e => e.Details).HasMaxLength(2000);

        b.HasOne(e => e.KpiCheckIn).WithMany(c => c.HistoryLogs)
            .HasForeignKey(e => e.KpiCheckInId).OnDelete(DeleteBehavior.Cascade);

        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── KPI Goal Comment ─────────────────────────────────────────────────────────
public class KpiGoalCommentConfiguration : IEntityTypeConfiguration<KpiGoalComment>
{
    public void Configure(EntityTypeBuilder<KpiGoalComment> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Content).HasMaxLength(3000).IsRequired();
        b.Property(e => e.CommentType).HasMaxLength(50);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── KPI Fail Reason ──────────────────────────────────────────────────────────
public class KpiFailReasonConfiguration : IEntityTypeConfiguration<KpiFailReason>
{
    public void Configure(EntityTypeBuilder<KpiFailReason> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.ReasonName).HasMaxLength(200).IsRequired();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── KPI Adjustment History ───────────────────────────────────────────────────
public class KpiAdjustmentHistoryConfiguration : IEntityTypeConfiguration<KpiAdjustmentHistory>
{
    public void Configure(EntityTypeBuilder<KpiAdjustmentHistory> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.FieldChanged).HasMaxLength(100).IsRequired();
        b.Property(e => e.OldValue).HasMaxLength(500);
        b.Property(e => e.NewValue).HasMaxLength(500);
        b.Property(e => e.Reason).HasMaxLength(1000);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── Evaluation Result ────────────────────────────────────────────────────────
public class EvaluationResultConfiguration : IEntityTypeConfiguration<EvaluationResult>
{
    public void Configure(EntityTypeBuilder<EvaluationResult> b)
    {
        b.HasKey(e => e.Id);
        b.HasIndex(e => new { e.UserId, e.EvaluationPeriodId }).IsUnique();
        b.Property(e => e.TotalScore).HasColumnType("decimal(5,2)");
        b.Property(e => e.Classification).HasMaxLength(50);
        b.Property(e => e.ReviewComment).HasMaxLength(2000);
        b.Property(e => e.DirectorReviewComment).HasMaxLength(2000);
        b.Property(e => e.SubmissionStatus).HasConversion<int>();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── One-on-One Meeting ───────────────────────────────────────────────────────
public class OneOnOneMeetingConfiguration : IEntityTypeConfiguration<OneOnOneMeeting>
{
    public void Configure(EntityTypeBuilder<OneOnOneMeeting> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Agenda).HasMaxLength(500);
        b.Property(e => e.Notes).HasMaxLength(3000);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── KPI Result Comparison ────────────────────────────────────────────────────
public class KpiResultComparisonConfiguration : IEntityTypeConfiguration<KpiResultComparison>
{
    public void Configure(EntityTypeBuilder<KpiResultComparison> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.TargetValue).HasColumnType("decimal(18,2)");
        b.Property(e => e.AchievedValue).HasColumnType("decimal(18,2)");
        b.Property(e => e.CompletionPercent).HasColumnType("decimal(5,2)");
        b.Property(e => e.Note).HasMaxLength(1000);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── Realtime Expected Bonus ──────────────────────────────────────────────────
public class RealtimeExpectedBonusConfiguration : IEntityTypeConfiguration<RealtimeExpectedBonus>
{
    public void Configure(EntityTypeBuilder<RealtimeExpectedBonus> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.EstimatedBonus).HasColumnType("decimal(18,2)");
        b.Property(e => e.CurrentScore).HasColumnType("decimal(5,2)");
        b.Property(e => e.EstimatedRank).HasMaxLength(50);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

// ── AI Generation History ────────────────────────────────────────────────────
public class AiGenerationHistoryConfiguration : IEntityTypeConfiguration<AiGenerationHistory>
{
    public void Configure(EntityTypeBuilder<AiGenerationHistory> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.FeatureType).HasMaxLength(100).IsRequired();
        b.Property(e => e.ModelName).HasMaxLength(200);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
