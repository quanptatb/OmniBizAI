using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;
using OmniBizAI.Domain.Entities.Finance;
using OmniBizAI.Domain.Entities.Performance;
using OmniBizAI.Domain.Entities.Workflow;
using OmniBizAI.Domain.Enums;
using OmniBizAI.Domain.Interfaces;

namespace OmniBizAI.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var transactions = _unitOfWork.Repository<Transaction>().Query().Where(x => !x.IsDeleted && x.Status == "Completed").ToList();
        var budgets = _unitOfWork.Repository<Budget>().Query().Where(x => !x.IsDeleted).ToList();
        var kpis = _unitOfWork.Repository<Kpi>().Query().Where(x => !x.IsDeleted && x.Status == "Active").ToList();
        var pending = _unitOfWork.Repository<WorkflowInstance>().Query().Count(x => x.Status == WorkflowStatus.InProgress || x.Status == WorkflowStatus.Pending);

        var risks = new List<string>();
        risks.AddRange(budgets.Where(x => x.UtilizationPercent >= 100).Select(x => $"{x.Name} vượt ngân sách {x.UtilizationPercent:0.##}%"));
        risks.AddRange(kpis.Where(x => x.Progress < 50).Select(x => $"{x.Name} mới đạt {x.Progress:0.##}%"));

        var dto = new DashboardOverviewDto(
            TotalIncome: transactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount),
            TotalExpense: transactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount),
            RemainingBudget: budgets.Sum(x => x.RemainingAmount),
            AverageKpiProgress: kpis.Count == 0 ? 0 : Math.Round(kpis.Average(x => x.Progress), 2),
            PendingApprovals: pending,
            RiskAlerts: risks.Take(5).ToList());

        return Task.FromResult(dto);
    }
}
