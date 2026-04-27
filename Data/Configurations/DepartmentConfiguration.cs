using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniBizAI.Models.Entities.Organization;

namespace OmniBizAI.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration cho entity Department.
/// Map với bảng 'Departments' đã tồn tại trong DB (PascalCase columns).
/// </summary>
public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        // Tên bảng khớp với DB hiện tại (PascalCase từ migration cũ)
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("Id");

        // Map các cột thực tế trong DB
        builder.Property(d => d.CompanyId).HasColumnName("CompanyId").IsRequired();
        builder.Property(d => d.Code).HasColumnName("Code").HasMaxLength(20).IsRequired();
        builder.Property(d => d.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();
        builder.Property(d => d.ParentDepartmentId).HasColumnName("ParentDepartmentId");
        builder.Property(d => d.BudgetLimit).HasColumnName("BudgetLimit").HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(d => d.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
        builder.Property(d => d.SortOrder).HasColumnName("SortOrder").HasDefaultValue(0);

        // Audit fields
        builder.Property(d => d.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(d => d.UpdatedAt).HasColumnName("UpdatedAt");
        builder.Property(d => d.CreatedBy).HasColumnName("CreatedBy");
        builder.Property(d => d.UpdatedBy).HasColumnName("UpdatedBy");

        // Soft delete
        builder.Property(d => d.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false);
        builder.Property(d => d.DeletedAt).HasColumnName("DeletedAt");

        // RowVersion không có trong DB cũ — bỏ qua
        builder.Ignore(d => d.RowVersion);

        // Relationships
        builder.HasOne(d => d.Company)
            .WithMany(c => c.Departments)
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global query filter
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
