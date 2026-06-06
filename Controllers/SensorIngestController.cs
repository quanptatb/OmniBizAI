using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.Services;

namespace OmniBizAI.Controllers;

/// <summary>F5.5 — Endpoint nhận reading từ IoT (API key per tenant qua header X-Tenant-Api-Key).</summary>
[ApiController]
[Route("api/sensor")]
[AllowAnonymous]
public class SensorIngestController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly SensorAnomalyDetector _detector;
    private readonly ILogger<SensorIngestController> _logger;

    public SensorIngestController(ApplicationDbContext db, SensorAnomalyDetector detector, ILogger<SensorIngestController> logger)
    {
        _db = db;
        _detector = detector;
        _logger = logger;
    }

    public record SensorIngestRequest(
        Guid TenantId,
        Guid EquipmentId,
        string SensorType,
        double Value,
        string? Unit,
        double? ThresholdWarning,
        double? ThresholdCritical,
        DateTimeOffset? ReadingTime);

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] SensorIngestRequest req, CancellationToken ct)
    {
        // API-key check qua header. Lưu API key trong TenantSetting key "SensorIngest.ApiKey" (đơn giản hoá ban đầu).
        var providedKey = Request.Headers["X-Tenant-Api-Key"].ToString();
        if (string.IsNullOrWhiteSpace(providedKey))
            return Unauthorized(new { error = "Missing X-Tenant-Api-Key header." });

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == req.TenantId && !t.IsDeleted, ct);
        if (tenant == null) return NotFound(new { error = "Tenant not found." });

        var expectedKey = await _db.TenantSettings
            .Where(s => s.TenantId == req.TenantId && !s.IsDeleted && s.Key == "SensorIngest.ApiKey")
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(expectedKey) || expectedKey != providedKey)
            return Unauthorized(new { error = "Invalid API key." });

        var equipment = await _db.Equipments
            .FirstOrDefaultAsync(e => e.Id == req.EquipmentId && e.TenantId == req.TenantId && !e.IsDeleted, ct);
        if (equipment == null) return NotFound(new { error = "Equipment not found." });

        var status = SensorReadingStatus.Normal;
        if (req.ThresholdCritical.HasValue && req.Value >= req.ThresholdCritical.Value)
            status = SensorReadingStatus.Critical;
        else if (req.ThresholdWarning.HasValue && req.Value >= req.ThresholdWarning.Value)
            status = SensorReadingStatus.Warning;

        var reading = new EquipmentSensorReading
        {
            TenantId = req.TenantId,
            EquipmentId = req.EquipmentId,
            SensorType = req.SensorType,
            Value = req.Value,
            Unit = req.Unit ?? "",
            ReadingTime = req.ReadingTime ?? DateTimeOffset.UtcNow,
            Status = status,
            ThresholdWarning = req.ThresholdWarning,
            ThresholdCritical = req.ThresholdCritical,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.EquipmentSensorReadings.Add(reading);
        await _db.SaveChangesAsync(ct);

        AnomalyResult? anomaly = null;
        try
        {
            anomaly = await _detector.EvaluateAsync(reading, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anomaly detection failed for reading {Id}", reading.Id);
        }

        return Ok(new
        {
            readingId = reading.Id,
            status = reading.Status.ToString(),
            anomaly = anomaly != null
                ? new { isAnomaly = anomaly.IsAnomaly, reason = anomaly.Reason, score = anomaly.Score, status = anomaly.Status.ToString() }
                : null
        });
    }
}
