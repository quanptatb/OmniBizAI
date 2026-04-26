using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities.Finance;

namespace OmniBizAI.Data.Configurations.Finance;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        // Table
        builder.ToTable("budgets");

        // Primary key
        builder.HasKey(b => b.Id);

        // Properties
        builder.Property(b => b.CompanyId)
            .IsRequired();

        builder.Property(b => b.FiscalPeriodId)
            .IsRequired();

        builder.Property(b => b.DepartmentId)
            .IsRequired();

        builder.Property(b => b.CategoryId)
            .IsRequired();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.AllocatedAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.SpentAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.CommittedAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.RemainingAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.UtilizationPct)
            .HasColumnType("decimal(5,2)");

        builder.Property(b => b.WarningThreshold)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(80m);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.Notes)
            .HasMaxLength(2000);

        // Audit / Soft-delete columns
        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.IsDeleted)
            .HasDefaultValue(false);

        // Query filter for soft delete
        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
