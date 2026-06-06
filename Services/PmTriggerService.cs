using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Services;

/// <summary>F5.4 — Quét PmSchedule và sinh WorkOrder Draft khi đạt ngưỡng (run-hours / cycles / condition).</summary>
public class PmTriggerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PmTriggerService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(60);

    public PmTriggerService(IServiceProvider serviceProvider, ILogger<PmTriggerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Đợi 90s sau khi ứng dụng start để DB sẵn sàng
        try { await Task.Delay(TimeSpan.FromSeconds(90), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PmTriggerService loop failed.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task ProcessOnceAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Lấy danh sách PmSchedule cần xét (không phải TimeBased — TimeBased đã có flow ExecutePm)
        var schedules = await db.PmSchedules
            .Include(p => p.Equipment)
            .Where(p => !p.IsDeleted && p.IsActive
                && p.TriggerType != PmTriggerType.TimeBased)
            .ToListAsync(ct);

        if (schedules.Count == 0) return;

        var byTenant = schedules.GroupBy(p => p.TenantId);
        var generated = 0;

        foreach (var tenantGroup in byTenant)
        {
            foreach (var pm in tenantGroup)
            {
                if (pm.Equipment == null) continue;
                if (!ShouldTrigger(pm, db, ct, out var reason)) continue;

                // Kiểm tra trùng: đã có WO draft từ PM này chưa kết thúc?
                var hasOpenWo = await db.WorkOrders.AnyAsync(w =>
                    w.TenantId == pm.TenantId && !w.IsDeleted
                    && w.PmScheduleId == pm.Id
                    && (w.Status == WorkOrderStatus.Open || w.Status == WorkOrderStatus.Assigned
                        || w.Status == WorkOrderStatus.InProgress || w.Status == WorkOrderStatus.OnHold),
                    ct);
                if (hasOpenWo) continue;

                var numbering = scope.ServiceProvider.GetService<INumberingService>();
                string code;
                if (numbering != null)
                {
                    // Numbering service yêu cầu ITenantContext gắn tenant — fallback bằng raw NumberSequence
                    code = await GenerateWorkOrderCodeAsync(db, pm.TenantId, ct);
                }
                else
                {
                    code = await GenerateWorkOrderCodeAsync(db, pm.TenantId, ct);
                }

                var wo = new WorkOrder
                {
                    TenantId = pm.TenantId,
                    Code = code,
                    EquipmentId = pm.EquipmentId,
                    Type = WorkOrderType.Preventive,
                    Status = pm.AssignedTechnicianId.HasValue ? WorkOrderStatus.Assigned : WorkOrderStatus.Open,
                    Priority = PriorityLevel.Normal,
                    Title = $"PM tự động: {pm.TaskName}",
                    Description = $"{pm.Instructions}\n\n[Trigger] {reason}",
                    AssignedTechnicianId = pm.AssignedTechnicianId,
                    ScheduledStart = DateTimeOffset.UtcNow,
                    EstimatedHours = pm.EstimatedDurationMinutes.HasValue ? (decimal)pm.EstimatedDurationMinutes.Value / 60m : null,
                    PmScheduleId = pm.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.WorkOrders.Add(wo);
                db.AuditLogs.Add(new AuditLog
                {
                    TenantId = pm.TenantId,
                    Action = "AutoGenFromTrigger",
                    EntityName = "WorkOrder",
                    EntityId = wo.Id,
                    NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        wo.Code,
                        wo.PmScheduleId,
                        TriggerType = pm.TriggerType.ToString(),
                        Reason = reason
                    }),
                    CreatedAt = DateTimeOffset.UtcNow
                });
                generated++;
            }
        }

        if (generated > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("PmTriggerService: tạo {Count} Work Order tự động.", generated);
        }
    }

    private static async Task<string> GenerateWorkOrderCodeAsync(ApplicationDbContext db, Guid tenantId, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var seq = await db.NumberSequences
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Code == NumberingSequenceKeys.WorkOrder && s.Year == year, ct);
        if (seq == null)
        {
            seq = new NumberSequence
            {
                TenantId = tenantId,
                Code = NumberingSequenceKeys.WorkOrder,
                Prefix = "WO-",
                PaddingLength = 4,
                Year = year,
                CurrentNumber = 1,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.NumberSequences.Add(seq);
        }
        else
        {
            seq.CurrentNumber += 1;
            seq.UpdatedAt = DateTimeOffset.UtcNow;
        }
        return $"WO-{year}-{seq.CurrentNumber:D4}";
    }

    private static bool ShouldTrigger(PmSchedule pm, ApplicationDbContext db, CancellationToken ct, out string reason)
    {
        reason = "";
        var eq = pm.Equipment!;

        switch (pm.TriggerType)
        {
            case PmTriggerType.RunHoursBased:
                if (!pm.IntervalHours.HasValue || pm.IntervalHours.Value <= 0) return false;
                var lastHours = pm.LastRunHoursAtPm ?? 0;
                var elapsedHours = eq.RunHours - lastHours;
                if (elapsedHours >= pm.IntervalHours.Value)
                {
                    reason = $"RunHours {eq.RunHours:N1} − last {lastHours:N1} = {elapsedHours:N1}h ≥ {pm.IntervalHours.Value:N1}h";
                    return true;
                }
                return false;

            case PmTriggerType.CyclesBased:
                if (!pm.IntervalCycles.HasValue || pm.IntervalCycles.Value <= 0) return false;
                var lastCycles = pm.LastCyclesAtPm ?? 0;
                var elapsedCycles = eq.CycleCount - lastCycles;
                if (elapsedCycles >= pm.IntervalCycles.Value)
                {
                    reason = $"Cycles {eq.CycleCount} − last {lastCycles} = {elapsedCycles} ≥ {pm.IntervalCycles.Value}";
                    return true;
                }
                return false;

            case PmTriggerType.ConditionBased:
                if (string.IsNullOrWhiteSpace(pm.ConditionSensorType) || !pm.ConditionThreshold.HasValue) return false;
                // Lấy reading mới nhất 1h gần đây cho sensor type
                var cutoff = DateTimeOffset.UtcNow.AddHours(-1);
                var lastReading = db.EquipmentSensorReadings
                    .Where(r => !r.IsDeleted && r.EquipmentId == pm.EquipmentId
                        && r.SensorType == pm.ConditionSensorType
                        && r.ReadingTime >= cutoff)
                    .OrderByDescending(r => r.ReadingTime)
                    .FirstOrDefault();
                if (lastReading != null && lastReading.Value >= pm.ConditionThreshold.Value)
                {
                    reason = $"Sensor {pm.ConditionSensorType}={lastReading.Value:N2} {lastReading.Unit} ≥ {pm.ConditionThreshold.Value:N2}";
                    return true;
                }
                return false;

            default:
                return false;
        }
    }
}
