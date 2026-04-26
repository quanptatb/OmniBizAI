using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities.Finance;

namespace OmniBizAI.Data.Configurations.Finance;

public class PaymentRequestItemConfiguration : IEntityTypeConfiguration<PaymentRequestItem>
{
    public void Configure(EntityTypeBuilder<PaymentRequestItem> builder)
    {
        // Table
        builder.ToTable("payment_request_items");

        // Primary key
        builder.HasKey(i => i.Id);

        // Properties
        builder.Property(i => i.PaymentRequestId)
            .IsRequired();

        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Quantity)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Unit)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Item");

        builder.Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.TotalPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.TaxRate)
            .HasColumnType("decimal(5,2)");

        builder.Property(i => i.TaxAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.SortOrder)
            .HasDefaultValue(0);
    }
}
