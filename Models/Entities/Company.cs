namespace OmniBizAI.Models.Entities;

public sealed class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<Department> Departments { get; set; } = [];
}
