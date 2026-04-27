using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities.Organization;

namespace OmniBizAI.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration cho entity Company.
/// Map với bảng 'Companies' đã tồn tại trong DB (PascalCase columns).
/// Actual schema: Id, Name, ShortName, TaxCode, Address, Phone, Email, Website,
///                DefaultCurrency, FiscalYearStartMonth, SettingsJson,
///                CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
/// </summary>
public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        // Tên bảng khớp với DB hiện tại (PascalCase từ migration cũ)
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("Id");

        // Map các cột thực tế trong DB
        builder.Property(c => c.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Currency).HasColumnName("DefaultCurrency").HasMaxLength(3).HasDefaultValue("VND");
        builder.Property(c => c.TaxCode).HasColumnName("TaxCode").HasMaxLength(30);
        builder.Property(c => c.Address).HasColumnName("Address").HasMaxLength(500);

        // Code không có trong DB cũ — dùng ShortName (nullable)
        builder.Property(c => c.Code).HasColumnName("ShortName").HasMaxLength(20).IsRequired(false);

        // Audit fields
        builder.Property(c => c.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(c => c.UpdatedAt).HasColumnName("UpdatedAt");
        builder.Property(c => c.CreatedBy).HasColumnName("CreatedBy");
        builder.Property(c => c.UpdatedBy).HasColumnName("UpdatedBy");

        // Các cột KHÔNG tồn tại trong bảng Companies (DB cũ) — bỏ qua
        builder.Ignore(c => c.IsActive);
        builder.Ignore(c => c.Industry);
        builder.Ignore(c => c.IsDeleted);
        builder.Ignore(c => c.DeletedAt);
        builder.Ignore(c => c.RowVersion);

        // Navigation
        builder.HasMany(c => c.Departments)
            .WithOne(d => d.Company)
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
