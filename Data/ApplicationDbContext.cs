using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<EvaluationPeriod> EvaluationPeriods { get; set; }
    public DbSet<Objective> Objectives { get; set; }
    public DbSet<Kpi> Kpis { get; set; }
    public DbSet<KpiCheckIn> KpiCheckIns { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
