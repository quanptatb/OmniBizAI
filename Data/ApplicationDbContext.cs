using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<EmployeeProfile> EmployeeProfiles { get; set; }
    public DbSet<EvaluationPeriod> EvaluationPeriods { get; set; }
    public DbSet<Objective> Objectives { get; set; }
    public DbSet<Kpi> Kpis { get; set; }
    public DbSet<KpiCheckIn> KpiCheckIns { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Tenant)
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Company>()
            .HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Department>()
            .HasOne(d => d.Company)
            .WithMany()
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Department>()
            .HasOne(d => d.Tenant)
            .WithMany()
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EmployeeProfile>()
            .HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Naming according to Technical Blueprint Database Blueprint
        builder.Entity<EvaluationPeriod>().ToTable("evaluation_periods");
        builder.Entity<Objective>().ToTable("objectives");
        builder.Entity<Kpi>().ToTable("kpis");
        builder.Entity<KpiCheckIn>().ToTable("kpi_check_ins");

        // KPI Relationships
        builder.Entity<Kpi>()
            .HasOne(k => k.EvaluationPeriod)
            .WithMany(e => e.Kpis)
            .HasForeignKey(k => k.EvaluationPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<KpiCheckIn>()
            .HasOne(c => c.Kpi)
            .WithMany(k => k.CheckIns)
            .HasForeignKey(c => c.KpiId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<Objective>()
            .HasOne(o => o.EvaluationPeriod)
            .WithMany(e => e.Objectives)
            .HasForeignKey(o => o.EvaluationPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
