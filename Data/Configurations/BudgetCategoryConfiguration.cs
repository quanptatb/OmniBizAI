using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities.Finance;

namespace OmniBizAI.Data.Configurations;

/// <summary>
/// EF Core configuration cho BudgetCategory.
/// Map với bảng 'BudgetCategories' đã tồn tại trong DB (PascalCase columns).
/// Schema DB: Id, CompanyId, ParentId, Name, Code, Type, Description, Color, IsActive, CreatedAt
/// </summary>
public sealed class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategory>
{
    public void Configure(EntityTypeBuilder<BudgetCategory> builder)
    {
        // Tên bảng khớp với DB hiện tại (PascalCase từ migration cũ)
        builder.ToTable("BudgetCategories");

        builder.HasKey(bc => bc.Id);
        builder.Property(bc => bc.Id).HasColumnName("Id");

        builder.Property(bc => bc.Code).HasColumnName("Code").HasMaxLength(50).IsRequired();
        builder.Property(bc => bc.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();
        builder.Property(bc => bc.Description).HasColumnName("Description").HasMaxLength(500);
        builder.Property(bc => bc.ParentCategoryId).HasColumnName("ParentId");
        builder.Property(bc => bc.IsActive).HasColumnName("IsActive").HasDefaultValue(true);

        // SortOrder không có trong DB cũ — bỏ qua
        builder.Ignore(bc => bc.SortOrder);

        // Audit fields (DB chỉ có CreatedAt)
        builder.Property(bc => bc.CreatedAt).HasColumnName("CreatedAt");
        builder.Ignore(bc => bc.UpdatedAt);
        builder.Ignore(bc => bc.CreatedBy);
        builder.Ignore(bc => bc.UpdatedBy);

        // Self-referencing relationship (tree)
        builder.HasOne(bc => bc.ParentCategory)
            .WithMany(bc => bc.ChildCategories)
            .HasForeignKey(bc => bc.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
