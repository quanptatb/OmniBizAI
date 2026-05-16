using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class SettingsService(ApplicationDbContext db, ITenantContext tenant)
{
    // ── Company ──────────────────────────────────────────────────────────────
    public async Task<CompanySettingsViewModel> GetCompanySettingsAsync()
    {
        var tid = tenant.TenantId;
        var t = await db.Tenants.FindAsync(tid);
        var bp = await db.BusinessProfiles.FirstOrDefaultAsync(b => b.TenantId == tid && b.IsDefault && !b.IsDeleted);

        return new CompanySettingsViewModel
        {
            TenantId = tid,
            CompanyName = t?.Name ?? "",
            TenantCode = t?.Code,
            BusinessType = t?.BusinessType,
            ProfileCode = bp?.Code,
            Industry = bp?.Industry
        };
    }

    public async Task<bool> SaveCompanySettingsAsync(CompanySettingsViewModel vm)
    {
        var t = await db.Tenants.FindAsync(tenant.TenantId);
        if (t is null) return false;

        t.Name = vm.CompanyName;
        t.BusinessType = vm.BusinessType;

        var bp = await db.BusinessProfiles.FirstOrDefaultAsync(b => b.TenantId == tenant.TenantId && b.IsDefault && !b.IsDeleted);
        if (bp != null)
        {
            bp.Code = vm.ProfileCode ?? bp.Code;
            bp.Industry = vm.Industry ?? bp.Industry;
            bp.UpdatedAt = DateTimeOffset.UtcNow;
        }

        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "UpdateSettings", EntityName = "Tenant", EntityId = tenant.TenantId, NewValuesJson = $"{{\"CompanyName\":\"{vm.CompanyName}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    // ── Modules ──────────────────────────────────────────────────────────────
    public async Task<ModuleListViewModel> GetModulesAsync()
    {
        var tid = tenant.TenantId;
        var items = await db.TenantModules.Where(m => m.TenantId == tid && !m.IsDeleted)
            .OrderBy(m => m.ModuleCode)
            .Select(m => new ModuleListItem
            {
                Id = m.Id, ModuleCode = m.ModuleCode, DisplayName = m.DisplayName,
                Status = m.Status.ToString(), EnabledFrom = m.EnabledFrom, EnabledTo = m.EnabledTo
            }).ToListAsync();

        return new ModuleListViewModel { Items = items };
    }

    public async Task<bool> ToggleModuleAsync(Guid id)
    {
        var m = await db.TenantModules.FindAsync(id);
        if (m is null || m.TenantId != tenant.TenantId) return false;

        m.Status = m.Status == ModuleStatus.Enabled ? ModuleStatus.Disabled : ModuleStatus.Enabled;
        m.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "ToggleModule", EntityName = "TenantModule", EntityId = id, NewValuesJson = $"{{\"Status\":\"{m.Status}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    // ── Parameters ───────────────────────────────────────────────────────────
    public async Task<ParameterListViewModel> GetParametersAsync()
    {
        var tid = tenant.TenantId;
        var params_ = await db.SystemParameters.Where(p => p.TenantId == tid && !p.IsDeleted)
            .OrderBy(p => p.Group).ThenBy(p => p.Key).ToListAsync();

        var groups = params_.GroupBy(p => p.Group).Select(g => new ParameterGroupItem
        {
            GroupName = g.Key,
            Parameters = g.Select(p => new ParameterItem
            {
                Id = p.Id, Key = p.Key, Value = p.Value, ValueType = p.ValueType, IsEditable = p.IsEditable
            }).ToList()
        }).ToList();

        return new ParameterListViewModel { Groups = groups };
    }

    public async Task<bool> UpdateParameterAsync(Guid id, string value)
    {
        var p = await db.SystemParameters.FindAsync(id);
        if (p is null || p.TenantId != tenant.TenantId || !p.IsEditable) return false;

        p.Value = value;
        p.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}
