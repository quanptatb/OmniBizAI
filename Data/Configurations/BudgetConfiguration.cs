using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities.Finance;

namespace OmniBizAI.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration cho entity Budget.
/// Map với bảng 'budgets' đã tồn tại trong DB.
/// Schema DB: Id, CompanyId, FiscalPeriodId, DepartmentId, CategoryId, Name,
///            AllocatedAmount, SpentAmount, CommittedAmount, RemainingAmount,
///            UtilizationPct, WarningThreshold, Status, Notes,
///            CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted, DeletedAt
/// (Không có BudgetCode và RowVersion)
/// </summary>
public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");

        // Primary key
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("Id").HasDefaultValueSql("NEWID()");

        // Foreign Keys
        builder.Property(b => b.CompanyId).HasColumnName("CompanyId").IsRequired();
        builder.Property(b => b.FiscalPeriodId).HasColumnName("FiscalPeriodId").IsRequired();
        builder.Property(b => b.DepartmentId).HasColumnName("DepartmentId").IsRequired();
        builder.Property(b => b.CategoryId).HasColumnName("CategoryId").IsRequired();

        // Business Fields
        builder.Property(b => b.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();

        // Financial fields — PascalCase columns
        builder.Property(b => b.AllocatedAmount)
            .HasColumnName("AllocatedAmount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(b => b.SpentAmount)
            .HasColumnName("SpentAmount")
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(b => b.CommittedAmount)
            .HasColumnName("CommittedAmount")
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(b => b.RemainingAmount)
            .HasColumnName("RemainingAmount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(b => b.UtilizationPct)
            .HasColumnName("UtilizationPct")
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0m);

        builder.Property(b => b.WarningThreshold)
            .HasColumnName("WarningThreshold")
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(80m);

        builder.Property(b => b.Status)
            .HasColumnName("Status")
            .HasMaxLength(20)
            .HasDefaultValue("Active");

        builder.Property(b => b.Notes).HasColumnName("Notes");

        // Audit Fields
        builder.Property(b => b.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(b => b.UpdatedAt).HasColumnName("UpdatedAt");
        builder.Property(b => b.CreatedBy).HasColumnName("CreatedBy");
        builder.Property(b => b.UpdatedBy).HasColumnName("UpdatedBy");

        // Soft Delete
        builder.Property(b => b.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
        builder.Property(b => b.DeletedAt).HasColumnName("DeletedAt");

        // RowVersion không có trong bảng budgets cũ — bỏ qua
        builder.Ignore(b => b.RowVersion);

        // Relationships
        builder.HasOne(b => b.Company)
            .WithMany()
            .HasForeignKey(b => b.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.FiscalPeriod)
            .WithMany(fp => fp.Budgets)
            .HasForeignKey(b => b.FiscalPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Department)
            .WithMany()
            .HasForeignKey(b => b.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Category)
            .WithMany(c => c.Budgets)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global Query Filter
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
