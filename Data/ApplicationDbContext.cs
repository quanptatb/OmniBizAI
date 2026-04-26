using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities.Finance;

namespace OmniBizAI.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    // Finance module
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<PaymentRequest> PaymentRequests => Set<PaymentRequest>();
    public DbSet<PaymentRequestItem> PaymentRequestItems => Set<PaymentRequestItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
