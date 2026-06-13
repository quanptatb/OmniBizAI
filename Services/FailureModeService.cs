using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

/// <summary>F5.6 — Failure Mode catalog + thống kê</summary>
public class FailureModeService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly INumberingService _numbering;
    private readonly IAuditService _audit;

    public FailureModeService(ApplicationDbContext db, ITenantContext tenant, INumberingService numbering, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _numbering = numbering;
        _audit = audit;
    }

    public async Task<List<FailureModeItem>> GetListAsync(string? search, FailureModeCategory? category, bool? activeOnly)
    {
        var tid = _tenant.TenantId;
        var q = _db.FailureModes.Where(f => f.TenantId == tid && !f.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(f => f.Code.Contains(search) || f.Name.Contains(search));
        if (category.HasValue) q = q.Where(f => f.Category == category.Value);
        if (activeOnly == true) q = q.Where(f => f.IsActive);

        return await q.OrderBy(f => f.Code).Select(f => new FailureModeItem
        {
            Id = f.Id,
            Code = f.Code,
            Name = f.Name,
            Category = f.Category,
            Description = f.Description,
            TypicalPreventionMeasure = f.TypicalPreventionMeasure,
            IsActive = f.IsActive,
            IncidentCount = f.Incidents.Count(i => !i.IsDeleted)
        }).ToListAsync();
    }

    public async Task<List<SelectOption>> GetActiveOptionsAsync()
    {
        var tid = _tenant.TenantId;
        return await _db.FailureModes
            .Where(f => f.TenantId == tid && !f.IsDeleted && f.IsActive)
            .OrderBy(f => f.Code)
            .Select(f => new SelectOption { Value = f.Id.ToString(), Text = $"{f.Code} — {f.Name}" })
            .ToListAsync();
    }

    public async Task<(bool Success, Guid Id, string Message)> CreateAsync(FailureModeEditViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Name)) return (false, Guid.Empty, "Tên bắt buộc.");
        var tid = _tenant.TenantId;

        var code = !string.IsNullOrWhiteSpace(vm.Code)
            ? vm.Code.Trim()
            : await _numbering.NextAsync(NumberingSequenceKeys.FailureMode, "FM-", 4);

        // Validate unique
        var exists = await _db.FailureModes.AnyAsync(f => f.TenantId == tid && !f.IsDeleted && f.Code == code);
        if (exists) return (false, Guid.Empty, $"Mã {code} đã tồn tại.");

        var entity = new FailureMode
        {
            TenantId = tid,
            Code = code,
            Name = vm.Name,
            Category = vm.Category,
            Description = vm.Description,
            TypicalPreventionMeasure = vm.TypicalPreventionMeasure,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId
        };
        _db.FailureModes.Add(entity);
        await _audit.LogAsync("FailureMode", entity.Id, "Create",
            newValueObj: new { entity.Code, entity.Name, entity.Category });
        await _db.SaveChangesAsync();
        return (true, entity.Id, $"Đã tạo {entity.Code}.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Guid id, FailureModeEditViewModel vm)
    {
        var tid = _tenant.TenantId;
        var entity = await _db.FailureModes.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tid && !f.IsDeleted);
        if (entity == null) return (false, "Không tìm thấy.");

        entity.Name = vm.Name;
        entity.Category = vm.Category;
        entity.Description = vm.Description;
        entity.TypicalPreventionMeasure = vm.TypicalPreventionMeasure;
        entity.IsActive = vm.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _audit.LogAsync("FailureMode", entity.Id, "Update",
            newValueObj: new { entity.Name, entity.Category, entity.IsActive });
        await _db.SaveChangesAsync();
        return (true, "Đã cập nhật.");
    }

    public async Task<FailureModeStatisticsViewModel> GetStatisticsAsync(int months = 6)
    {
        var tid = _tenant.TenantId;
        var cutoff = DateTimeOffset.UtcNow.AddMonths(-months);

        var data = await _db.MaintenanceIncidents
            .Where(i => i.TenantId == tid && !i.IsDeleted
                && i.FailureModeId.HasValue
                && i.CreatedAt >= cutoff)
            .Include(i => i.FailureMode)
            .GroupBy(i => new { i.FailureModeId, i.FailureMode!.Code, i.FailureMode.Name, i.FailureMode.Category })
            .Select(g => new FailureModeStatItem
            {
                FailureModeId = g.Key.FailureModeId!.Value,
                Code = g.Key.Code,
                Name = g.Key.Name,
                Category = g.Key.Category,
                IncidentCount = g.Count(),
                TotalDowntimeHours = g.Sum(i => i.DowntimeHours ?? 0m)
            })
            .OrderByDescending(s => s.IncidentCount)
            .Take(10)
            .ToListAsync();

        var totalIncidents = await _db.MaintenanceIncidents
            .CountAsync(i => i.TenantId == tid && !i.IsDeleted && i.CreatedAt >= cutoff);
        var taggedIncidents = await _db.MaintenanceIncidents
            .CountAsync(i => i.TenantId == tid && !i.IsDeleted && i.FailureModeId.HasValue && i.CreatedAt >= cutoff);

        return new FailureModeStatisticsViewModel
        {
            Months = months,
            TotalIncidents = totalIncidents,
            TaggedIncidents = taggedIncidents,
            TopFailureModes = data
        };
    }
}
