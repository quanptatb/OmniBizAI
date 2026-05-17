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

    public async Task<Guid> CreateParameterAsync(string group, string key, string? value, string valueType = "String")
    {
        var p = new SystemParameter
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            Group = group,
            Key = key,
            Value = value,
            ValueType = valueType,
            IsEditable = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = tenant.UserId
        };
        db.SystemParameters.Add(p);
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "SystemParameter", EntityId = p.Id, NewValuesJson = $"{{\"Group\":\"{group}\",\"Key\":\"{key}\",\"Value\":\"{value}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return p.Id;
    }

    public async Task<bool> DeleteParameterAsync(Guid id)
    {
        var p = await db.SystemParameters.FindAsync(id);
        if (p is null || p.TenantId != tenant.TenantId) return false;
        p.IsDeleted = true; p.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Delete", EntityName = "SystemParameter", EntityId = id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    // ── Enum Label Management ────────────────────────────────────────────────
    
    /// <summary>
    /// Load enum label overrides from SystemParameter (Group starts with "EnumLabel.") and cache.
    /// </summary>
    public async Task LoadEnumLabelsAsync()
    {
        var tid = tenant.TenantId;
        var labelParams = await db.SystemParameters
            .Where(p => p.TenantId == tid && !p.IsDeleted && p.Group.StartsWith("EnumLabel."))
            .ToListAsync();

        var overrides = new Dictionary<string, string>();
        foreach (var lp in labelParams)
        {
            // Group = "EnumLabel.OperationStatus", Key = "Draft", Value = "Bản nháp"
            var enumType = lp.Group.Replace("EnumLabel.", "");
            var lookupKey = $"{enumType}.{lp.Key}";
            overrides[lookupKey] = lp.Value ?? lp.Key;
        }
        Helpers.EnumLabels.SetTenantOverrides(tid, overrides);
    }

    /// <summary>
    /// Get all enum labels (defaults + overrides) for management UI.
    /// </summary>
    public async Task<EnumLabelManagementViewModel> GetEnumLabelsAsync()
    {
        var tid = tenant.TenantId;
        var defaults = Helpers.EnumLabels.GetAllDefaults();

        // Load current overrides from DB
        var dbLabels = await db.SystemParameters
            .Where(p => p.TenantId == tid && !p.IsDeleted && p.Group.StartsWith("EnumLabel."))
            .ToListAsync();

        var dbLookup = dbLabels.ToDictionary(p => $"{p.Group.Replace("EnumLabel.", "")}.{p.Key}", p => new { p.Id, p.Value });

        var groups = new List<EnumLabelGroupVm>();
        foreach (var (enumName, values) in defaults)
        {
            var items = new List<EnumLabelItemVm>();
            foreach (var (fullKey, defaultLabel) in values)
            {
                var enumValueName = fullKey.Split('.')[1];
                var dbEntry = dbLookup.ContainsKey(fullKey) ? dbLookup[fullKey] : null;
                items.Add(new EnumLabelItemVm
                {
                    FullKey = fullKey,
                    EnumValue = enumValueName,
                    DefaultLabel = defaultLabel,
                    CustomLabel = dbEntry?.Value,
                    ParameterId = dbEntry?.Id,
                    IsOverridden = dbEntry != null
                });
            }
            groups.Add(new EnumLabelGroupVm
            {
                EnumTypeName = enumName,
                DisplayName = Helpers.EnumLabels.GetGroupDisplayName(enumName),
                Items = items
            });
        }
        return new EnumLabelManagementViewModel { Groups = groups };
    }

    /// <summary>
    /// Save enum label overrides. Creates or updates SystemParameter records.
    /// </summary>
    public async Task SaveEnumLabelsAsync(Dictionary<string, string> labels)
    {
        var tid = tenant.TenantId;
        var existing = await db.SystemParameters
            .Where(p => p.TenantId == tid && !p.IsDeleted && p.Group.StartsWith("EnumLabel."))
            .ToListAsync();

        foreach (var (fullKey, label) in labels)
        {
            var parts = fullKey.Split('.', 2);
            if (parts.Length != 2) continue;
            var group = $"EnumLabel.{parts[0]}";
            var key = parts[1];

            var exist = existing.FirstOrDefault(e => e.Group == group && e.Key == key);
            if (exist != null)
            {
                exist.Value = label;
                exist.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                db.SystemParameters.Add(new SystemParameter
                {
                    Id = Guid.NewGuid(), TenantId = tid, Group = group, Key = key,
                    Value = label, ValueType = "String", IsEditable = true,
                    CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = tenant.UserId
                });
            }
        }

        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "UpdateEnumLabels", EntityName = "SystemParameter", EntityId = tid, NewValuesJson = $"{{\"Count\":{labels.Count}}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        // Refresh cache
        Helpers.EnumLabels.ClearTenantOverrides(tid);
        await LoadEnumLabelsAsync();
    }

    // ── Theme / Appearance ───────────────────────────────────────────────────

    private static readonly Dictionary<string, string> _themeDefaults = new()
    {
        ["AccentColor"] = "#0066cc", ["AccentHover"] = "#0071e3", ["AccentDark"] = "#2997ff",
        ["CanvasColor"] = "#ffffff", ["ParchmentColor"] = "#f5f5f7", ["InkColor"] = "#1d1d1f",
        ["SidebarBg"] = "#1d1d1f", ["SidebarText"] = "#ffffff",
        ["SuccessColor"] = "#34c759", ["WarningColor"] = "#ff9f0a", ["DangerColor"] = "#ff3b30", ["InfoColor"] = "#5ac8fa",
        ["FontFamily"] = "Inter", ["FontSize"] = "17",
        ["BorderRadius"] = "12", ["SidebarWidth"] = "260",
        ["LogoIcon"] = "fa-solid fa-brain", ["BrandName"] = "OmniBizAI", ["BrandTagline"] = "Smart Operations",
        ["DarkMode"] = "false"
    };

    public async Task<ThemeSettingsViewModel> GetThemeAsync()
    {
        var tid = tenant.TenantId;
        var themeParams = await db.SystemParameters
            .Where(p => p.TenantId == tid && !p.IsDeleted && p.Group == "Theme")
            .ToDictionaryAsync(p => p.Key, p => p.Value ?? "");

        return new ThemeSettingsViewModel
        {
            AccentColor = themeParams.GetValueOrDefault("AccentColor", _themeDefaults["AccentColor"]),
            AccentHover = themeParams.GetValueOrDefault("AccentHover", _themeDefaults["AccentHover"]),
            AccentDark = themeParams.GetValueOrDefault("AccentDark", _themeDefaults["AccentDark"]),
            CanvasColor = themeParams.GetValueOrDefault("CanvasColor", _themeDefaults["CanvasColor"]),
            ParchmentColor = themeParams.GetValueOrDefault("ParchmentColor", _themeDefaults["ParchmentColor"]),
            InkColor = themeParams.GetValueOrDefault("InkColor", _themeDefaults["InkColor"]),
            SidebarBg = themeParams.GetValueOrDefault("SidebarBg", _themeDefaults["SidebarBg"]),
            SidebarText = themeParams.GetValueOrDefault("SidebarText", _themeDefaults["SidebarText"]),
            SuccessColor = themeParams.GetValueOrDefault("SuccessColor", _themeDefaults["SuccessColor"]),
            WarningColor = themeParams.GetValueOrDefault("WarningColor", _themeDefaults["WarningColor"]),
            DangerColor = themeParams.GetValueOrDefault("DangerColor", _themeDefaults["DangerColor"]),
            InfoColor = themeParams.GetValueOrDefault("InfoColor", _themeDefaults["InfoColor"]),
            FontFamily = themeParams.GetValueOrDefault("FontFamily", _themeDefaults["FontFamily"]),
            FontSize = themeParams.GetValueOrDefault("FontSize", _themeDefaults["FontSize"]),
            BorderRadius = themeParams.GetValueOrDefault("BorderRadius", _themeDefaults["BorderRadius"]),
            SidebarWidth = themeParams.GetValueOrDefault("SidebarWidth", _themeDefaults["SidebarWidth"]),
            LogoIcon = themeParams.GetValueOrDefault("LogoIcon", _themeDefaults["LogoIcon"]),
            BrandName = themeParams.GetValueOrDefault("BrandName", _themeDefaults["BrandName"]),
            BrandTagline = themeParams.GetValueOrDefault("BrandTagline", _themeDefaults["BrandTagline"]),
            DarkMode = themeParams.GetValueOrDefault("DarkMode", "false") == "true"
        };
    }

    public async Task SaveThemeAsync(ThemeSettingsViewModel vm)
    {
        var tid = tenant.TenantId;
        var existing = await db.SystemParameters
            .Where(p => p.TenantId == tid && !p.IsDeleted && p.Group == "Theme")
            .ToListAsync();

        var values = new Dictionary<string, string>
        {
            ["AccentColor"] = vm.AccentColor, ["AccentHover"] = vm.AccentHover, ["AccentDark"] = vm.AccentDark,
            ["CanvasColor"] = vm.CanvasColor, ["ParchmentColor"] = vm.ParchmentColor, ["InkColor"] = vm.InkColor,
            ["SidebarBg"] = vm.SidebarBg, ["SidebarText"] = vm.SidebarText,
            ["SuccessColor"] = vm.SuccessColor, ["WarningColor"] = vm.WarningColor, ["DangerColor"] = vm.DangerColor, ["InfoColor"] = vm.InfoColor,
            ["FontFamily"] = vm.FontFamily, ["FontSize"] = vm.FontSize,
            ["BorderRadius"] = vm.BorderRadius, ["SidebarWidth"] = vm.SidebarWidth,
            ["LogoIcon"] = vm.LogoIcon, ["BrandName"] = vm.BrandName, ["BrandTagline"] = vm.BrandTagline,
            ["DarkMode"] = vm.DarkMode ? "true" : "false"
        };

        foreach (var (key, value) in values)
        {
            var exist = existing.FirstOrDefault(e => e.Key == key);
            if (exist != null) { exist.Value = value; exist.UpdatedAt = DateTimeOffset.UtcNow; }
            else
            {
                db.SystemParameters.Add(new SystemParameter
                {
                    Id = Guid.NewGuid(), TenantId = tid, Group = "Theme", Key = key,
                    Value = value, ValueType = "String", IsEditable = true,
                    CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = tenant.UserId
                });
            }
        }

        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "UpdateTheme", EntityName = "SystemParameter", EntityId = tid, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Generate CSS override string for the current tenant's theme.
    /// </summary>
    public async Task<string> GetThemeCssAsync()
    {
        var vm = await GetThemeAsync();
        var css = $@":root {{
    --apple-blue: {vm.AccentColor};
    --apple-blue-focus: {vm.AccentHover};
    --apple-blue-dark: {vm.AccentDark};
    --canvas: {vm.CanvasColor};
    --parchment: {vm.ParchmentColor};
    --ink: {vm.InkColor};
    --text-primary: {vm.InkColor};
    --success: {vm.SuccessColor};
    --warning: {vm.WarningColor};
    --danger: {vm.DangerColor};
    --info: {vm.InfoColor};
    --r-md: {vm.BorderRadius}px;
    --sidebar-w: {vm.SidebarWidth}px;
}}
html {{ font-size: {vm.FontSize}px; }}
body,.app-body {{ font-family: '{vm.FontFamily}', system-ui, -apple-system, sans-serif; }}
.sidebar {{ background: {vm.SidebarBg}; }}
.sidebar, .sidebar .nav-item, .sidebar .nav-section-label {{ color: {vm.SidebarText}; }}";

        if (vm.DarkMode)
        {
            css += @"
:root {
    --canvas: #1c1c1e; --parchment: #000000; --ink: #f5f5f7;
    --text-primary: #f5f5f7; --text-secondary: #a1a1a6; --text-muted: #8e8e93;
    --divider: #38383a; --hairline: #48484a;
    --pearl: #2c2c2e;
}
body,.app-body { background: #000; color: #f5f5f7; }
.content-card,.stat-card,.card,.glass-card { background: #1c1c1e; border-color: #38383a; }
.card-header-custom { background: #2c2c2e; border-color: #38383a; }
.data-table thead { background: #2c2c2e; }
.data-table tbody tr:hover { background: #2c2c2e; }
.form-control,.form-select,.form-input { background: #2c2c2e; color: #f5f5f7; border-color: #48484a; }
.topbar { background: rgba(28,28,30,.85); border-color: #38383a; }
.modal-content { background: #1c1c1e; color: #f5f5f7; }
.btn-light { background: #2c2c2e; color: #f5f5f7; border-color: #48484a; }
.dropdown-menu { background: #2c2c2e; border-color: #48484a; }
.dropdown-item { color: #f5f5f7; }
.dropdown-item:hover { background: #3a3a3c; }";
        }
        return css;
    }
}

