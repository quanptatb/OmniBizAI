namespace OmniBizAI.Models.Entities;

public class KpiCheckIn : SoftDeletableEntity
{
    public Guid KpiId { get; set; }
    public virtual Kpi Kpi { get; set; } = null!;
    
    public decimal PreviousValue { get; set; }
    public decimal NewValue { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Rejected
    
    public string? Comment { get; set; } // Bắt buộc khi Reject
}
