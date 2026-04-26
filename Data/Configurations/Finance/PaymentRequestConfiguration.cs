using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities.Finance;

namespace OmniBizAI.Data.Configurations.Finance;

public class PaymentRequestConfiguration : IEntityTypeConfiguration<PaymentRequest>
{
    public void Configure(EntityTypeBuilder<PaymentRequest> builder)
    {
        // Table
        builder.ToTable("payment_requests");

        // Primary key
        builder.HasKey(pr => pr.Id);

        // Properties
        builder.Property(pr => pr.CompanyId)
            .IsRequired();

        builder.Property(pr => pr.RequestNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pr => pr.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(pr => pr.Description)
            .HasMaxLength(2000);

        builder.Property(pr => pr.DepartmentId)
            .IsRequired();

        builder.Property(pr => pr.RequesterId)
            .IsRequired();

        builder.Property(pr => pr.CategoryId)
            .IsRequired();

        builder.Property(pr => pr.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(pr => pr.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("VND");

        builder.Property(pr => pr.Priority)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pr => pr.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pr => pr.AiRiskScore)
            .HasColumnType("decimal(5,2)");

        builder.Property(pr => pr.AiRiskLevel)
            .HasMaxLength(50);

        builder.Property(pr => pr.Notes)
            .HasMaxLength(2000);

        // Audit / Soft-delete columns
        builder.Property(pr => pr.CreatedAt)
            .IsRequired();

        builder.Property(pr => pr.IsDeleted)
            .HasDefaultValue(false);

        // Query filter for soft delete
        builder.HasQueryFilter(pr => !pr.IsDeleted);

        // Concurrency token (SQL Server rowversion)
        builder.Property(pr => pr.RowVersion)
            .IsRowVersion();

        // Relationships
        builder.HasMany(pr => pr.Items)
            .WithOne(i => i.PaymentRequest)
            .HasForeignKey(i => i.PaymentRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index
        builder.HasIndex(pr => pr.RequestNumber)
            .IsUnique();
    }
}
