namespace OmniBizAI.Models.Entities;

public class EvaluationPeriod : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Planning"; // Planning, Active, Closed
    
    public virtual ICollection<Kpi> Kpis { get; set; } = new List<Kpi>();
    public virtual ICollection<Objective> Objectives { get; set; } = new List<Objective>();
}
