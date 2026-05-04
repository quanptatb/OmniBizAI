namespace OmniBizAI.Models.Entities;

public class Kpi : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Progress { get; set; }
    public string Direction { get; set; } = "IncreaseIsBetter"; // IncreaseIsBetter, DecreaseIsBetter
    public string Status { get; set; } = "Draft"; // Draft, Active, Paused, Completed, Cancelled
    
    public Guid EvaluationPeriodId { get; set; }
    public virtual EvaluationPeriod EvaluationPeriod { get; set; } = null!;
    
    public Guid OwnerId { get; set; } // Liên kết đến User/Employee
    
    public virtual ICollection<KpiCheckIn> CheckIns { get; set; } = new List<KpiCheckIn>();
}
