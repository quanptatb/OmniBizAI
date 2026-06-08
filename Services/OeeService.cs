using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;

namespace OmniBizAI.Services;

public record OeeMetrics(double Availability, double Performance, double Quality)
{
    public double Oee => Availability * Performance * Quality;
}

/// <summary>
/// Tính OEE (Overall Equipment Effectiveness) = Availability × Performance × Quality.
/// </summary>
public class OeeService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public OeeService(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<OeeMetrics> GetOeeAsync(Guid equipmentId, DateOnly from, DateOnly to)
    {
        var tid = _tenant.TenantId;

        var avail = await _db.EquipmentAvailabilityLogs
            .Where(a => a.TenantId == tid && !a.IsDeleted
                && a.EquipmentId == equipmentId
                && a.LogDate >= from && a.LogDate <= to)
            .GroupBy(_ => 1)
            .Select(g => new { Planned = g.Sum(x => x.PlannedMinutes), Down = g.Sum(x => x.DowntimeMinutes) })
            .FirstOrDefaultAsync();

        var availability = avail == null || avail.Planned == 0
            ? 0
            : (double)(avail.Planned - avail.Down) / avail.Planned;

        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        var runs = await _db.ProductionRuns
            .Where(r => r.TenantId == tid && !r.IsDeleted
                && r.EquipmentId == equipmentId
                && r.StartedAt >= fromDt && r.StartedAt <= toDt
                && r.CompletedAt.HasValue)
            .Select(r => new
            {
                RunSeconds = (r.CompletedAt!.Value - r.StartedAt).TotalSeconds,
                r.IdealCycleSeconds,
                r.GoodCount,
                r.RejectCount
            })
            .ToListAsync();

        double idealOutput = runs.Sum(r =>
            r.IdealCycleSeconds > 0 ? r.RunSeconds / (double)r.IdealCycleSeconds : 0);
        int actualOutput = runs.Sum(r => r.GoodCount + r.RejectCount);
        int goodOutput = runs.Sum(r => r.GoodCount);
        int totalOutput = actualOutput;

        var performance = idealOutput > 0 ? actualOutput / idealOutput : 0;
        if (performance > 1) performance = 1;
        var quality = totalOutput > 0 ? (double)goodOutput / totalOutput : 0;

        return new OeeMetrics(availability, performance, quality);
    }
}
