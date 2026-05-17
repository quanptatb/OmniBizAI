using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

/// <summary>
/// Service for database backup and restore operations.
/// Supports SQL Server BACKUP DATABASE / RESTORE DATABASE commands,
/// backup history tracking, file management, and data export.
/// </summary>
public class BackupService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly string _backupDir;

    public BackupService(ApplicationDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db; _config = config; _env = env;
        _backupDir = Path.Combine(env.ContentRootPath, "App_Data", "Backups");
        if (!Directory.Exists(_backupDir)) Directory.CreateDirectory(_backupDir);
    }

    // ── Create Backup ────────────────────────────────────────────────────────

    /// <summary>Create a SQL Server backup file (.bak).</summary>
    public async Task<BackupResultViewModel> CreateBackupAsync(string? description = null, string type = "Full")
    {
        var result = new BackupResultViewModel();
        var dbName = _db.Database.GetDbConnection().Database;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{dbName}_{type}_{timestamp}.bak";
        var filePath = Path.Combine(_backupDir, fileName);

        try
        {
            var connStr = _config.GetConnectionString("DefaultConnection")!;
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            var sql = type == "Differential"
                ? $"BACKUP DATABASE [{dbName}] TO DISK = N'{filePath}' WITH DIFFERENTIAL, FORMAT, INIT, NAME = N'{dbName}-Differential-{timestamp}', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
                : $"BACKUP DATABASE [{dbName}] TO DISK = N'{filePath}' WITH FORMAT, INIT, NAME = N'{dbName}-Full-{timestamp}', SKIP, NOREWIND, NOUNLOAD, STATS = 10";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 600; // 10 minutes
            await cmd.ExecuteNonQueryAsync();

            var fi = new FileInfo(filePath);
            result.Success = true;
            result.FileName = fileName;
            result.FilePath = filePath;
            result.FileSize = fi.Length;
            result.Message = $"Sao lưu {type} thành công: {fileName} ({FormatSize(fi.Length)})";
            result.BackupType = type;
            result.Description = description;
            result.CreatedAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Lỗi sao lưu: {ex.Message}";
            // Cleanup failed file
            if (File.Exists(filePath)) try { File.Delete(filePath); } catch { }
        }

        return result;
    }

    // ── List Backups ─────────────────────────────────────────────────────────

    /// <summary>Get all backup files in the backup directory.</summary>
    public Task<BackupDashboardViewModel> GetBackupDashboardAsync()
    {
        var files = Directory.Exists(_backupDir)
            ? Directory.GetFiles(_backupDir, "*.bak")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new BackupFileItem
                {
                    FileName = f.Name,
                    FilePath = f.FullName,
                    FileSize = f.Length,
                    FileSizeDisplay = FormatSize(f.Length),
                    CreatedAt = f.CreationTime,
                    BackupType = f.Name.Contains("_Differential_") ? "Differential" : "Full",
                    DatabaseName = f.Name.Split('_')[0]
                }).ToList()
            : new List<BackupFileItem>();

        // Also scan for JSON exports
        var jsonFiles = Directory.Exists(_backupDir)
            ? Directory.GetFiles(_backupDir, "*.json")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new BackupFileItem
                {
                    FileName = f.Name,
                    FilePath = f.FullName,
                    FileSize = f.Length,
                    FileSizeDisplay = FormatSize(f.Length),
                    CreatedAt = f.CreationTime,
                    BackupType = "JSON Export",
                    DatabaseName = f.Name.Split('_')[0]
                }).ToList()
            : new List<BackupFileItem>();

        var allFiles = files.Concat(jsonFiles).OrderByDescending(f => f.CreatedAt).ToList();

        var vm = new BackupDashboardViewModel
        {
            BackupFiles = allFiles,
            TotalBackups = allFiles.Count,
            TotalSize = allFiles.Sum(f => f.FileSize),
            TotalSizeDisplay = FormatSize(allFiles.Sum(f => f.FileSize)),
            LastBackupAt = allFiles.FirstOrDefault()?.CreatedAt,
            BackupDirectory = _backupDir,
            DatabaseName = _db.Database.GetDbConnection().Database,
            SqlBackupCount = files.Count,
            JsonExportCount = jsonFiles.Count
        };
        return Task.FromResult(vm);
    }

    // ── Download Backup ──────────────────────────────────────────────────────

    /// <summary>Get file stream for download.</summary>
    public (Stream? stream, string fileName, string contentType)? GetBackupFileForDownload(string fileName)
    {
        var filePath = Path.Combine(_backupDir, fileName);
        if (!File.Exists(filePath)) return null;
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = fileName.EndsWith(".json") ? "application/json" : "application/octet-stream";
        return (stream, fileName, contentType);
    }

    // ── Delete Backup ────────────────────────────────────────────────────────

    public bool DeleteBackup(string fileName)
    {
        var filePath = Path.Combine(_backupDir, fileName);
        if (!File.Exists(filePath)) return false;
        // Prevent path traversal
        if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(_backupDir))) return false;
        File.Delete(filePath);
        return true;
    }

    // ── Restore Backup ───────────────────────────────────────────────────────

    /// <summary>Restore database from a .bak file. DANGEROUS — requires exclusive access.</summary>
    public async Task<BackupResultViewModel> RestoreBackupAsync(string fileName)
    {
        var result = new BackupResultViewModel();
        var filePath = Path.Combine(_backupDir, fileName);

        if (!File.Exists(filePath))
        {
            result.Success = false;
            result.Message = "File backup không tồn tại.";
            return result;
        }

        var dbName = _db.Database.GetDbConnection().Database;
        try
        {
            var connStr = _config.GetConnectionString("DefaultConnection")!;
            // Connect to master to restore
            var masterConnStr = new SqlConnectionStringBuilder(connStr) { InitialCatalog = "master" }.ConnectionString;
            await using var conn = new SqlConnection(masterConnStr);
            await conn.OpenAsync();

            // Set single-user mode
            var setSingle = $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
            await using (var cmd1 = new SqlCommand(setSingle, conn)) { cmd1.CommandTimeout = 120; await cmd1.ExecuteNonQueryAsync(); }

            // Restore
            var restoreSql = $"RESTORE DATABASE [{dbName}] FROM DISK = N'{filePath}' WITH REPLACE, STATS = 10";
            await using (var cmd2 = new SqlCommand(restoreSql, conn)) { cmd2.CommandTimeout = 600; await cmd2.ExecuteNonQueryAsync(); }

            // Set multi-user mode
            var setMulti = $"ALTER DATABASE [{dbName}] SET MULTI_USER";
            await using (var cmd3 = new SqlCommand(setMulti, conn)) { cmd3.CommandTimeout = 30; await cmd3.ExecuteNonQueryAsync(); }

            result.Success = true;
            result.Message = $"Khôi phục thành công từ {fileName}.";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Lỗi khôi phục: {ex.Message}";
            // Try to restore multi-user mode
            try
            {
                var connStr = _config.GetConnectionString("DefaultConnection")!;
                var masterConnStr = new SqlConnectionStringBuilder(connStr) { InitialCatalog = "master" }.ConnectionString;
                await using var conn2 = new SqlConnection(masterConnStr);
                await conn2.OpenAsync();
                await using var cmd = new SqlCommand($"ALTER DATABASE [{dbName}] SET MULTI_USER", conn2);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* best effort */ }
        }

        return result;
    }

    // ── JSON Data Export ─────────────────────────────────────────────────────

    /// <summary>Export tenant data as a portable JSON file.</summary>
    public async Task<BackupResultViewModel> ExportTenantDataAsync(Guid tenantId)
    {
        var result = new BackupResultViewModel();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"TenantExport_{timestamp}.json";
        var filePath = Path.Combine(_backupDir, fileName);

        try
        {
            var data = new Dictionary<string, object>();

            // Core entities
            data["OrganizationUnits"] = await _db.OrganizationUnits.Where(e => e.TenantId == tenantId && !e.IsDeleted).ToListAsync();
            data["Positions"] = await _db.Positions.Where(e => e.TenantId == tenantId && !e.IsDeleted).ToListAsync();
            data["AppUsers"] = await _db.AppUsers.Where(e => e.TenantId == tenantId && !e.IsDeleted).Select(u => new { u.Id, u.FullName, u.Email, u.JobTitle, u.Status, u.OrganizationUnitId }).ToListAsync();
            data["Customers"] = await _db.Customers.Where(e => e.TenantId == tenantId && !e.IsDeleted).ToListAsync();
            data["Vendors"] = await _db.Vendors.Where(e => e.TenantId == tenantId && !e.IsDeleted).ToListAsync();
            data["Products"] = await _db.ProductServices.Where(e => e.TenantId == tenantId && !e.IsDeleted).ToListAsync();

            // Operations
            data["OperationRequests"] = await _db.OperationRequests.Where(e => e.TenantId == tenantId && !e.IsDeleted).Select(r => new { r.Id, r.RequestNo, r.Title, r.Type, r.Status, r.Priority, r.CreatedAt, r.DueDate, r.TotalAmount }).ToListAsync();

            // Finance
            data["Budgets"] = await _db.Budgets.Where(e => e.TenantId == tenantId && !e.IsDeleted).ToListAsync();
            data["Expenses"] = await _db.Expenses.Where(e => e.TenantId == tenantId && !e.IsDeleted).ToListAsync();
            data["CashTransactions"] = await _db.CashTransactions.Where(e => e.TenantId == tenantId && !e.IsDeleted).Select(t => new { t.Id, t.TransactionNo, t.TransactionType, t.Category, t.Amount, t.TransactionDate, t.Status }).ToListAsync();

            // KPI/OKR
            data["OkrObjectives"] = await _db.OkrObjectives.Where(e => e.TenantId == tenantId && !e.IsDeleted).ToListAsync();
            data["OkrKeyResults"] = await _db.OkrKeyResults.Where(e => e.TenantId == tenantId && !e.IsDeleted).ToListAsync();
            data["KpiDefinitions"] = await _db.KpiDefinitions.Where(e => e.TenantId == tenantId && !e.IsDeleted).ToListAsync();

            // Metadata
            data["_metadata"] = new { ExportedAt = DateTime.UtcNow, TenantId = tenantId, Version = "1.0", EntityCounts = data.ToDictionary(kv => kv.Key, kv => (kv.Value as System.Collections.ICollection)?.Count ?? 0) };

            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            var fi = new FileInfo(filePath);
            result.Success = true;
            result.FileName = fileName;
            result.FilePath = filePath;
            result.FileSize = fi.Length;
            result.Message = $"Xuất dữ liệu JSON thành công: {fileName} ({FormatSize(fi.Length)})";
            result.BackupType = "JSON Export";
            result.CreatedAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Lỗi xuất dữ liệu: {ex.Message}";
        }

        return result;
    }

    // ── Database Info ────────────────────────────────────────────────────────

    /// <summary>Get database size and metadata.</summary>
    public async Task<DatabaseInfoViewModel> GetDatabaseInfoAsync()
    {
        var vm = new DatabaseInfoViewModel { DatabaseName = _db.Database.GetDbConnection().Database };
        try
        {
            var connStr = _config.GetConnectionString("DefaultConnection")!;
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            // DB size
            await using var cmd = new SqlCommand("SELECT SUM(size * 8 / 1024) AS SizeMB FROM sys.database_files", conn);
            var sizeMb = await cmd.ExecuteScalarAsync();
            vm.SizeMB = sizeMb != null && sizeMb != DBNull.Value ? Convert.ToDecimal(sizeMb) : 0;
            vm.SizeDisplay = vm.SizeMB > 1024 ? $"{vm.SizeMB / 1024:F1} GB" : $"{vm.SizeMB:F0} MB";

            // Table count
            await using var cmd2 = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'", conn);
            vm.TableCount = (int)(await cmd2.ExecuteScalarAsync() ?? 0);

            // Last backup from SQL Server metadata
            await using var cmd3 = new SqlCommand($"SELECT TOP 1 backup_finish_date FROM msdb.dbo.backupset WHERE database_name = @db ORDER BY backup_finish_date DESC", conn);
            cmd3.Parameters.AddWithValue("@db", vm.DatabaseName);
            var lastBackup = await cmd3.ExecuteScalarAsync();
            vm.LastSqlBackupAt = lastBackup != null && lastBackup != DBNull.Value ? (DateTime?)lastBackup : null;

            // Server version
            await using var cmd4 = new SqlCommand("SELECT @@VERSION", conn);
            var ver = (await cmd4.ExecuteScalarAsync())?.ToString();
            vm.ServerVersion = ver?.Split('\n').FirstOrDefault()?.Trim() ?? "Unknown";
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = ex.Message;
        }

        return vm;
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    /// <summary>Delete backups older than specified days.</summary>
    public int CleanupOldBackups(int keepDays = 30)
    {
        if (!Directory.Exists(_backupDir)) return 0;
        var cutoff = DateTime.Now.AddDays(-keepDays);
        var deleted = 0;
        foreach (var file in Directory.GetFiles(_backupDir))
        {
            if (File.GetCreationTime(file) < cutoff)
            {
                try { File.Delete(file); deleted++; } catch { }
            }
        }
        return deleted;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1_024) return $"{bytes / 1_024.0:F1} KB";
        return $"{bytes} B";
    }
}
