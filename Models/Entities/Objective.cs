namespace OmniBizAI.Models.Entities;

public class Objective : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, Active, Completed, Cancelled
    public decimal Progress { get; set; }
    
    public Guid EvaluationPeriodId { get; set; }
    public virtual EvaluationPeriod EvaluationPeriod { get; set; } = null!;
}
