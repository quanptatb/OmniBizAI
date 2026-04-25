using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class Company
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? ShortName { get; set; }

    public string? TaxCode { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string DefaultCurrency { get; set; } = null!;

    public int FiscalYearStartMonth { get; set; }

    public string SettingsJson { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ICollection<BudgetCategory> BudgetCategories { get; set; } = new List<BudgetCategory>();

    public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<EvaluationPeriod> EvaluationPeriods { get; set; } = new List<EvaluationPeriod>();

    public virtual ICollection<FiscalPeriod> FiscalPeriods { get; set; } = new List<FiscalPeriod>();

    public virtual ICollection<Kpi> Kpis { get; set; } = new List<Kpi>();

    public virtual ICollection<Objective> Objectives { get; set; } = new List<Objective>();

    public virtual ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ICollection<Vendor> Vendors { get; set; } = new List<Vendor>();

    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
}
