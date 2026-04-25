using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Data;

public partial class ApplicationDbContext
{
    public virtual DbSet<DepartmentBudgetAllocation> DepartmentBudgetAllocations { get; set; }

    public virtual DbSet<BudgetLineItem> BudgetLineItems { get; set; }

    public virtual DbSet<VendorContact> VendorContacts { get; set; }

    public virtual DbSet<VendorBankAccount> VendorBankAccounts { get; set; }

    public virtual DbSet<VendorRating> VendorRatings { get; set; }

    public virtual DbSet<TransactionTag> TransactionTags { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<PaymentRequestComment> PaymentRequestComments { get; set; }

    public virtual DbSet<KpiTemplate> KpiTemplates { get; set; }

    public virtual DbSet<KrCheckIn> KrCheckIns { get; set; }

    public virtual DbSet<EvaluationScore> EvaluationScores { get; set; }

    public virtual DbSet<KpiTargetsHistory> KpiTargetsHistories { get; set; }

    public virtual DbSet<OkrAlignment> OkrAlignments { get; set; }

    public virtual DbSet<KpiComment> KpiComments { get; set; }

    public virtual DbSet<AiEmbedding> AiEmbeddings { get; set; }

    public virtual DbSet<AiPromptTemplate> AiPromptTemplates { get; set; }

    public virtual DbSet<EmailQueue> EmailQueues { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<BackgroundJob> BackgroundJobs { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepartmentBudgetAllocation>(entity =>
        {
            entity.ToTable("DepartmentBudgetAllocations");
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AllocatedAmount).HasColumnType("decimal(18, 2)");
            entity.HasIndex(e => e.DepartmentId);
            entity.HasOne<Department>().WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.ApprovedBy);
        });

        modelBuilder.Entity<BudgetLineItem>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PlannedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ActualAmount).HasColumnType("decimal(18, 2)");
            entity.HasIndex(e => e.BudgetId);
            entity.HasOne<Budget>().WithMany().HasForeignKey(e => e.BudgetId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<VendorContact>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ContactName).HasMaxLength(200);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.HasIndex(e => e.VendorId);
            entity.HasOne<Vendor>().WithMany().HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<VendorBankAccount>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BankName).HasMaxLength(200);
            entity.Property(e => e.AccountNumber).HasMaxLength(30);
            entity.Property(e => e.AccountHolder).HasMaxLength(200);
            entity.Property(e => e.Branch).HasMaxLength(200);
            entity.Property(e => e.SwiftCode).HasMaxLength(20);
            entity.HasIndex(e => e.VendorId);
            entity.HasOne<Vendor>().WithMany().HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<VendorRating>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Score).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.Criteria).HasMaxLength(50);
            entity.HasIndex(e => e.VendorId);
            entity.HasOne<Vendor>().WithMany().HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.RatedBy);
        });

        modelBuilder.Entity<TransactionTag>(entity =>
        {
            entity.HasKey(e => new { e.TransactionId, e.Tag });
            entity.Property(e => e.Tag).HasMaxLength(50);
            entity.HasOne<Transaction>().WithMany().HasForeignKey(e => e.TransactionId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.HasIndex(e => e.PermissionId);
            entity.HasOne<Role>().WithMany().HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne<Permission>().WithMany().HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PaymentRequestComment>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CommentType).HasMaxLength(20);
            entity.HasIndex(e => e.PaymentRequestId);
            entity.HasOne<PaymentRequest>().WithMany().HasForeignKey(e => e.PaymentRequestId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<KpiTemplate>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(300);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.MetricType).HasMaxLength(20);
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.DefaultTarget).HasColumnType("decimal(18, 2)");
            entity.HasIndex(e => e.CompanyId);
            entity.HasOne<Company>().WithMany().HasForeignKey(e => e.CompanyId);
        });

        modelBuilder.Entity<KrCheckIn>(entity =>
        {
            entity.ToTable("KrCheckIns");
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.PreviousValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NewValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Progress).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Confidence).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.HasIndex(e => e.KeyResultId);
            entity.HasOne<KeyResult>().WithMany().HasForeignKey(e => e.KeyResultId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.ReviewedBy);
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.SubmittedBy);
        });

        modelBuilder.Entity<EvaluationScore>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SourceType).HasMaxLength(10);
            entity.Property(e => e.SourceName).HasMaxLength(300);
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Score).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Rating).HasMaxLength(5);
            entity.HasIndex(e => e.EvaluationId);
            entity.HasOne<PerformanceEvaluation>().WithMany().HasForeignKey(e => e.EvaluationId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<KpiTargetsHistory>(entity =>
        {
            entity.ToTable("KpiTargetsHistory");
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.OldTarget).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NewTarget).HasColumnType("decimal(18, 2)");
            entity.HasIndex(e => e.KpiId);
            entity.HasOne<Kpi>().WithMany().HasForeignKey(e => e.KpiId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.ChangedBy);
        });

        modelBuilder.Entity<OkrAlignment>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AlignmentType).HasMaxLength(20);
            entity.Property(e => e.ContributionWeight).HasColumnType("decimal(5, 2)");
            entity.HasOne<Objective>().WithMany().HasForeignKey(e => e.SourceObjectiveId);
            entity.HasOne<Objective>().WithMany().HasForeignKey(e => e.TargetObjectiveId);
        });

        modelBuilder.Entity<KpiComment>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CommentType).HasMaxLength(20);
            entity.HasIndex(e => e.KpiId);
            entity.HasOne<Kpi>().WithMany().HasForeignKey(e => e.KpiId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<AiEmbedding>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SourceType).HasMaxLength(50);
            entity.HasIndex(e => e.CompanyId);
            entity.HasOne<Company>().WithMany().HasForeignKey(e => e.CompanyId);
        });

        modelBuilder.Entity<AiPromptTemplate>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(50);
        });

        modelBuilder.Entity<EmailQueue>(entity =>
        {
            entity.ToTable("EmailQueue");
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ToEmail).HasMaxLength(255);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.TemplateName).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasIndex(e => new { e.CompanyId, e.Key }).IsUnique();
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.ValueType).HasMaxLength(20);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.HasOne<Company>().WithMany().HasForeignKey(e => e.CompanyId);
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UpdatedBy);
        });

        modelBuilder.Entity<BackgroundJob>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.JobType).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20);
        });
    }
}
