using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class Attachment : TenantEntity
{
    [StringLength(150)]
    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    [StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    public Guid UploadedByUserId { get; set; }
    public AppUser? UploadedByUser { get; set; }
}
