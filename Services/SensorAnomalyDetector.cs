using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Services;

public record AnomalyResult(bool IsAnomaly, string Reason, double Score, SensorReadingStatus Status);

/// <summary>F5.5 — Phát hiện bất thường cảm biến: moving average + 3σ + linear trend</summary>
public class SensorAnomalyDetector
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;
    private readonly NotificationService _notifications;

    private const int RollingWindowHours = 24;
    private const double SigmaMultiplier = 3.0;
    private const double TrendSlopeWarning = 0.5; // tăng > 0.5 đơn vị / giờ → cảnh báo

    public SensorAnomalyDetector(
        ApplicationDbContext db,
        ITenantContext tenant,
        IAuditService audit,
        NotificationService notifications)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
        _notifications = notifications;
    }

    /// <summary>Phân tích 1 reading mới. Nếu bất thường → tạo MaintenanceIncident (nếu chưa có Open).</summary>
    public async Task<AnomalyResult> EvaluateAsync(EquipmentSensorReading reading, CancellationToken ct = default)
    {
        var tid = reading.TenantId;
        var cutoff = reading.ReadingTime.AddHours(-RollingWindowHours);

        var history = await _db.EquipmentSensorReadings
            .Where(r => r.TenantId == tid && !r.IsDeleted
                && r.EquipmentId == reading.EquipmentId
                && r.SensorType == reading.SensorType
                && r.ReadingTime >= cutoff
                && r.Id != reading.Id)
            .OrderBy(r => r.ReadingTime)
            .Select(r => new { r.Value, r.ReadingTime })
            .ToListAsync(ct);

        if (history.Count < 8)
            return new AnomalyResult(false, "Chưa đủ dữ liệu 24h.", 0, SensorReadingStatus.Normal);

        var values = history.Select(h => h.Value).ToArray();
        var mean = values.Average();
        var stdDev = Math.Sqrt(values.Select(v => Math.Pow(v - mean, 2)).Sum() / values.Length);
        var diff = reading.Value - mean;
        var zScore = stdDev > 0 ? Math.Abs(diff) / stdDev : 0;

        // Linear regression slope (x = hours from start)
        var startTime = history.First().ReadingTime;
        var xs = history.Select(h => (h.ReadingTime - startTime).TotalHours).ToArray();
        var slope = LinearSlope(xs, values);

        // Detect anomaly:
        // 1. Spike: |z| > 3σ
        // 2. Trend tăng nhanh
        // 3. Reading vượt ngưỡng Critical
        var isSpike = zScore > SigmaMultiplier;
        var isTrend = Math.Abs(slope) > TrendSlopeWarning && reading.Value > mean;
        var isOverThreshold = reading.ThresholdCritical.HasValue && reading.Value >= reading.ThresholdCritical.Value;

        if (!isSpike && !isTrend && !isOverThreshold)
            return new AnomalyResult(false, $"Normal (z={zScore:N2}, slope={slope:N3}/h)", zScore, SensorReadingStatus.Normal);

        var reasonParts = new List<string>();
        if (isSpike) reasonParts.Add($"Spike {zScore:N1}σ (giá trị {reading.Value:N2}, mean {mean:N2}, σ {stdDev:N2})");
        if (isTrend) reasonParts.Add($"Trend tăng {slope:N2}/h");
        if (isOverThreshold) reasonParts.Add($"Vượt critical {reading.ThresholdCritical:N2}");
        var reason = string.Join("; ", reasonParts);

        var status = isOverThreshold ? SensorReadingStatus.Critical : SensorReadingStatus.Warning;
        await CreateIncidentIfNeededAsync(reading, reason, status, ct);
        return new AnomalyResult(true, reason, zScore, status);
    }

    private static double LinearSlope(double[] xs, double[] ys)
    {
        var n = xs.Length;
        if (n < 2) return 0;
        var mx = xs.Average();
        var my = ys.Average();
        var num = 0d;
        var den = 0d;
        for (var i = 0; i < n; i++)
        {
            num += (xs[i] - mx) * (ys[i] - my);
            den += (xs[i] - mx) * (xs[i] - mx);
        }
        return den == 0 ? 0 : num / den;
    }

    private async Task CreateIncidentIfNeededAsync(
        EquipmentSensorReading reading, string reason, SensorReadingStatus status, CancellationToken ct)
    {
        var tid = reading.TenantId;
        // Đã có incident Open cho equipment này trong 6h gần đây?
        var sixHoursAgo = DateTimeOffset.UtcNow.AddHours(-6);
        var hasOpenIncident = await _db.MaintenanceIncidents.AnyAsync(i =>
            i.TenantId == tid && !i.IsDeleted
            && i.EquipmentId == reading.EquipmentId
            && i.IsAnomalyDetected
            && i.CreatedAt >= sixHoursAgo
            && i.Status != IncidentStatus.Resolved
            && i.Status != IncidentStatus.Closed, ct);
        if (hasOpenIncident) return;

        var severity = status == SensorReadingStatus.Critical ? IncidentSeverity.High : IncidentSeverity.Medium;
        var incident = new MaintenanceIncident
        {
            TenantId = tid,
            EquipmentId = reading.EquipmentId,
            Title = $"[Auto] Bất thường {reading.SensorType} = {reading.Value:N2} {reading.Unit}",
            Description = reason,
            Severity = severity,
            Status = IncidentStatus.Open,
            OccurredAt = reading.ReadingTime,
            IsAnomalyDetected = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MaintenanceIncidents.Add(incident);

        await _audit.LogAsync("MaintenanceIncident", incident.Id, "AutoFromSensorAnomaly",
            newValueObj: new { incident.EquipmentId, incident.Severity, SensorType = reading.SensorType, reading.Value, Reason = reason });
        await _db.SaveChangesAsync(ct);

        try
        {
            await _notifications.SendToManagersAsync(
                $"Sensor anomaly: {reading.SensorType}",
                $"{reason} — Equipment {reading.EquipmentId}",
                "MaintenanceIncident", incident.Id);
        }
        catch { /* swallow - notification optional */ }
    }
}
