using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Finance;
using OmniBizAI.Models.Entities.Organization;

namespace OmniBizAI.Data;

/// <summary>
/// DbContext chính của OmniBizAI — kế thừa IdentityDbContext cho Auth.
/// Blueprint mục 3.5 và 5.1:
/// - Bảng nghiệp vụ dùng snake_case (Fluent API)
/// - Entity C# dùng PascalCase
/// - Soft delete global query filter
/// - Audit fields tự động
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    // ========================================
    // Organization (Stub)
    // ========================================
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();

    // ========================================
    // Finance
    // ========================================
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Áp dụng tất cả EF configuration từ thư mục Data/Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    /// <summary>
    /// Override SaveChanges để tự động set audit fields (created_at, updated_at).
    /// Blueprint mục 5.1: Thời gian lưu UTC.
    /// </summary>
    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync để tự động set audit fields.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Tự động gán giá trị cho các trường audit khi thêm/sửa entity.
    /// - Added: set CreatedAt = UTC now
    /// - Modified: set UpdatedAt = UTC now
    /// </summary>
    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}
