using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Services;

public class OperationAttachmentService(
    ApplicationDbContext db,
    ITenantContext tenant,
    OperationRequestService operationRequests,
    IAuditService audit,
    IWebHostEnvironment environment)
{
    public const string OperationRequestEntityName = "OperationRequest";
    private const long MaxFileSize = 25L * 1024 * 1024;
    private const int MaxFilesPerUpload = 10;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".png", ".jpg", ".jpeg", ".webp", ".txt", ".csv", ".zip"
    };

    public async Task<(bool Success, string Message)> UploadAsync(Guid requestId, IReadOnlyList<IFormFile> files)
    {
        var requestExists = await db.OperationRequests
            .AnyAsync(r => r.Id == requestId && r.TenantId == tenant.TenantId && !r.IsDeleted);
        if (!requestExists) return (false, "Không tìm thấy yêu cầu.");

        if (!await operationRequests.CanSupportOperationRequestAsync(requestId))
            return (false, "Bạn không có quyền đính kèm tài liệu cho yêu cầu này.");

        var validFiles = files.Where(f => f.Length > 0).ToList();
        if (!validFiles.Any()) return (false, "Chọn ít nhất một file để tải lên.");
        if (validFiles.Count > MaxFilesPerUpload)
            return (false, $"Chỉ được tải tối đa {MaxFilesPerUpload} file mỗi lần.");

        foreach (var file in validFiles)
        {
            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(originalFileName))
                return (false, "Tên file không hợp lệ.");
            if (file.Length > MaxFileSize)
                return (false, $"File {originalFileName} vượt quá giới hạn 25 MB.");
            if (!AllowedExtensions.Contains(extension))
                return (false, $"Định dạng file {extension} chưa được hỗ trợ.");
        }

        var now = DateTimeOffset.UtcNow;
        var relativeFolder = Path.Combine("operation-requests", tenant.TenantId.ToString("N"), requestId.ToString("N"));
        var fullFolder = ResolveStoragePath(relativeFolder);
        Directory.CreateDirectory(fullFolder);
        var storedPaths = new List<string>();

        try
        {
            foreach (var file in validFiles)
            {
                var originalFileName = TrimFileName(Path.GetFileName(file.FileName));
                var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
                var storedFileName = $"{Guid.NewGuid():N}{extension}";
                var relativePath = Path.Combine(relativeFolder, storedFileName);
                var storagePath = ToStoragePath(relativePath);
                var fullPath = ResolveStoragePath(relativePath);

                await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await file.CopyToAsync(stream);
                }

                storedPaths.Add(storagePath);
                db.Attachments.Add(new Attachment
                {
                    TenantId = tenant.TenantId,
                    EntityName = OperationRequestEntityName,
                    EntityId = requestId,
                    FileName = originalFileName,
                    StoragePath = storagePath,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : TrimContentType(file.ContentType),
                    FileSize = file.Length,
                    UploadedByUserId = tenant.UserId,
                    CreatedByUserId = tenant.UserId,
                    CreatedAt = now
                });
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            foreach (var path in storedPaths) TryDeletePhysicalFile(path);
            return (false, "Không thể lưu file lên hệ thống.");
        }

        await audit.LogAsync(OperationRequestEntityName, requestId, "UploadAttachment",
            newValueObj: new { Count = validFiles.Count, Files = validFiles.Select(f => Path.GetFileName(f.FileName)).ToList() });

        if (await db.SaveChangesWithConcurrencyAsync())
            return (true, validFiles.Count == 1 ? "Đã tải lên tài liệu." : $"Đã tải lên {validFiles.Count} tài liệu.");

        foreach (var path in storedPaths) TryDeletePhysicalFile(path);
        return (false, "Không thể lưu thông tin tài liệu do dữ liệu đã thay đổi.");
    }

    public async Task<OperationAttachmentDownload?> OpenAsync(Guid attachmentId)
    {
        var attachment = await db.Attachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId
                && a.TenantId == tenant.TenantId
                && a.EntityName == OperationRequestEntityName
                && !a.IsDeleted);

        if (attachment is null) return null;

        var requestExists = await db.OperationRequests
            .AnyAsync(r => r.Id == attachment.EntityId && r.TenantId == tenant.TenantId && !r.IsDeleted);
        if (!requestExists) return null;

        var fullPath = ResolveStoragePath(attachment.StoragePath);
        if (!File.Exists(fullPath)) return null;

        return new OperationAttachmentDownload(
            attachment.EntityId,
            attachment.FileName,
            attachment.ContentType ?? "application/octet-stream",
            new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public async Task<(bool Success, string Message, Guid? RequestId)> DeleteAsync(Guid attachmentId)
    {
        var attachment = await db.Attachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId
                && a.TenantId == tenant.TenantId
                && a.EntityName == OperationRequestEntityName
                && !a.IsDeleted);

        if (attachment is null) return (false, "Không tìm thấy tài liệu.", null);
        if (!await operationRequests.CanSupportOperationRequestAsync(attachment.EntityId))
            return (false, "Bạn không có quyền xóa tài liệu này.", attachment.EntityId);

        attachment.IsDeleted = true;
        attachment.UpdatedAt = DateTimeOffset.UtcNow;
        attachment.UpdatedByUserId = tenant.UserId;

        await audit.LogAsync(OperationRequestEntityName, attachment.EntityId, "DeleteAttachment",
            oldValueObj: new { attachment.FileName, attachment.FileSize });

        if (!await db.SaveChangesWithConcurrencyAsync())
            return (false, "Không thể xóa tài liệu do dữ liệu đã thay đổi.", attachment.EntityId);

        TryDeletePhysicalFile(attachment.StoragePath);
        return (true, "Đã xóa tài liệu.", attachment.EntityId);
    }

    private string ResolveStoragePath(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "uploads"));
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đường dẫn lưu file không hợp lệ.");
        return fullPath;
    }

    private void TryDeletePhysicalFile(string storagePath)
    {
        try
        {
            var fullPath = ResolveStoragePath(storagePath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch
        {
            // Metadata is already soft-deleted; missing files should not block the business flow.
        }
    }

    private static string ToStoragePath(string path) =>
        path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

    private static string TrimFileName(string fileName) =>
        fileName.Length <= 260 ? fileName : fileName[..260];

    private static string TrimContentType(string contentType) =>
        contentType.Length <= 100 ? contentType : contentType[..100];
}

public sealed record OperationAttachmentDownload(
    Guid RequestId,
    string FileName,
    string ContentType,
    Stream Stream);
