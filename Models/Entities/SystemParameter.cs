using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class SystemParameter : TenantEntity
{
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [StringLength(200)]
    public string Group { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Value { get; set; }

    [StringLength(50)]
    public string ValueType { get; set; } = "String";

    public bool IsEditable { get; set; } = true;
}
