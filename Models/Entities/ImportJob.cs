using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class ImportJob : TenantEntity
{
    public Guid UploadedByUserId { get; set; }
    public AppUser? UploadedByUser { get; set; }

    [StringLength(80)]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    public ImportJobStatus Status { get; set; } = ImportJobStatus.Uploaded;

    public int TotalRows { get; set; }

    public int SuccessRows { get; set; }

    public int ErrorRows { get; set; }

    public ICollection<ImportStagingRow> StagingRows { get; set; } = new List<ImportStagingRow>();
}
