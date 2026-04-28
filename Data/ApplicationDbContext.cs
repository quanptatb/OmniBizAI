using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<PaymentRequest> PaymentRequests => Set<PaymentRequest>();
    public DbSet<PaymentRequestItem> PaymentRequestItems => Set<PaymentRequestItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        builder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.HasOne(x => x.Company).WithMany(x => x.Departments).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EmployeeCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.EmploymentType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.CompanyId, x.EmployeeCode }).IsUnique();
            entity.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department).WithMany(x => x.Employees).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BudgetCategory>(entity =>
        {
            entity.ToTable("BudgetCategories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Vendor>(entity =>
        {
            entity.ToTable("Vendors");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TaxCode).HasMaxLength(50);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Rating).HasPrecision(3, 2);
            entity.HasIndex(x => new { x.CompanyId, x.Name });
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Budget>(entity =>
        {
            entity.ToTable("budgets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            entity.Property(x => x.SpentAmount).HasPrecision(18, 2);
            entity.Property(x => x.CommittedAmount).HasPrecision(18, 2);
            entity.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            entity.Property(x => x.UtilizationPct).HasPrecision(5, 2);
            entity.Property(x => x.WarningThreshold).HasPrecision(5, 2);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.DepartmentId, x.CategoryId, x.Name });
            entity.HasIndex(x => x.Status);
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category).WithMany(x => x.Budgets).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentRequest>(entity =>
        {
            entity.ToTable("payment_requests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RequestNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.AiRiskScore).HasPrecision(5, 2);
            entity.Property(x => x.AiRiskLevel).HasMaxLength(20);
            entity.Property<byte[]>("RowVersion").IsRowVersion();
            entity.HasIndex(x => x.RequestNumber).IsUnique().HasDatabaseName("IX_payment_requests_request_number");
            entity.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("IX_payment_requests_status_created_at");
            entity.HasIndex(x => new { x.DepartmentId, x.Status }).HasDatabaseName("IX_payment_requests_department_status");
            entity.HasIndex(x => x.RequesterId).HasDatabaseName("IX_payment_requests_requester_id");
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department).WithMany(x => x.PaymentRequests).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Requester).WithMany(x => x.PaymentRequests).HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vendor).WithMany(x => x.PaymentRequests).HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Budget).WithMany(x => x.PaymentRequests).HasForeignKey(x => x.BudgetId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category).WithMany(x => x.PaymentRequests).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentRequestItem>(entity =>
        {
            entity.ToTable("payment_request_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(10, 2);
            entity.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.TotalPrice).HasPrecision(18, 2);
            entity.Property(x => x.TaxRate).HasPrecision(5, 2);
            entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
            entity.HasOne(x => x.PaymentRequest).WithMany(x => x.Items).HasForeignKey(x => x.PaymentRequestId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
