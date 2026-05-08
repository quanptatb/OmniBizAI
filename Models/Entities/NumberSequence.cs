using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class NumberSequence : TenantEntity
{
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(30)]
    public string Prefix { get; set; } = string.Empty;

    public int CurrentNumber { get; set; }

    public int PaddingLength { get; set; } = 4;

    public int Year { get; set; }
}
