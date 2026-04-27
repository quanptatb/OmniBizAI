using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities.Finance;

namespace OmniBizAI.Data.Configurations;

/// <summary>
/// EF Core configuration cho FiscalPeriod.
/// Map với bảng 'FiscalPeriods' đã tồn tại trong DB (PascalCase columns).
/// </summary>
public sealed class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        // Tên bảng khớp với DB hiện tại (PascalCase từ migration cũ)
        builder.ToTable("FiscalPeriods");

        builder.HasKey(fp => fp.Id);
        builder.Property(fp => fp.Id).HasColumnName("Id");

        // DB dùng cột 'Type' thay vì 'Code' + 'PeriodType'
        // Code map vào cột 'Name', PeriodType map vào 'Type'
        builder.Property(fp => fp.Name).HasColumnName("Name").HasMaxLength(100).IsRequired();
        builder.Property(fp => fp.PeriodType).HasColumnName("Type").HasMaxLength(20).IsRequired();
        builder.Property(fp => fp.StartDate).HasColumnName("StartDate").IsRequired();
        builder.Property(fp => fp.EndDate).HasColumnName("EndDate").IsRequired();
        builder.Property(fp => fp.Status).HasColumnName("Status").HasMaxLength(20).HasDefaultValue("Active");
        builder.Property(fp => fp.CompanyId).HasColumnName("CompanyId").IsRequired();

        // Code không có trong DB — bỏ qua (generate runtime nếu cần)
        builder.Ignore(fp => fp.Code);

        // Audit fields
        builder.Property(fp => fp.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(fp => fp.UpdatedAt).HasColumnName("UpdatedAt");
        builder.Property(fp => fp.CreatedBy).HasColumnName("CreatedBy");
        builder.Property(fp => fp.UpdatedBy).HasColumnName("UpdatedBy");
    }
}
