using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class ImportStagingRow : TenantEntity
{
    public Guid ImportJobId { get; set; }
    public ImportJob? ImportJob { get; set; }

    public int RowNumber { get; set; }

    public string RawDataJson { get; set; } = string.Empty;

    public string? NormalizedDataJson { get; set; }

    public string? ErrorJson { get; set; }

    public bool IsValid { get; set; }

    public bool IsCommitted { get; set; }
}
