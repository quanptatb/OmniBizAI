using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;
using OmniBizAI.Domain.Entities.Organization;
using OmniBizAI.Domain.Entities.Performance;
using OmniBizAI.Domain.Enums;
using OmniBizAI.Domain.Interfaces;
using OmniBizAI.Domain.Rules;

namespace OmniBizAI.Application.Services;

public sealed class PerformanceService : IPerformanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public PerformanceService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<EvaluationPeriodDto> CreatePeriodAsync(CreateEvaluationPeriodRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndDate <= request.StartDate)
        {
            throw new BusinessRuleException("Evaluation period end date must be after start date.");
        }

        var overlap = _unitOfWork.Repository<EvaluationPeriod>().Query()
            .Any(x => x.Type == request.Type && x.StartDate <= request.EndDate && request.StartDate <= x.EndDate);
        if (overlap)
        {
            throw new BusinessRuleException("Evaluation period overlaps an existing period of the same type.");
        }

        var period = new EvaluationPeriod
        {
            CompanyId = GetCompanyId(),
            Name = request.Name.Trim(),
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        await _unitOfWork.Repository<EvaluationPeriod>().AddAsync(period, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapPeriod(period);
    }

    public async Task<ObjectiveDto> CreateObjectiveAsync(CreateObjectiveRequest request, CancellationToken cancellationToken = default)
    {
        var objective = new Objective
        {
            CompanyId = GetCompanyId(),
            Title = request.Title.Trim(),
            Description = request.Description,
            PeriodId = request.PeriodId,
            ParentId = request.ParentId,
            OwnerType = request.OwnerType,
            DepartmentId = request.DepartmentId,
            OwnerId = request.OwnerId,
            DueDate = request.DueDate,
            Status = "Active",
            CreatedBy = _currentUserService.UserId
        };

        await _unitOfWork.Repository<Objective>().AddAsync(objective, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapObjective(objective);
    }

    public async Task<KeyResultDto> CreateKeyResultAsync(CreateKeyResultRequest request, CancellationToken cancellationToken = default)
    {
        var keyResults = _unitOfWork.Repository<KeyResult>().Query().Where(x => x.ObjectiveId == request.ObjectiveId).ToList();
        if (keyResults.Sum(x => x.Weight) + request.Weight > 100)
        {
            throw new BusinessRuleException("Total key result weight for an objective cannot exceed 100%.");
        }

        var keyResult = new KeyResult
        {
            ObjectiveId = request.ObjectiveId,
            Title = request.Title.Trim(),
            MetricType = request.MetricType,
            Unit = request.Unit,
            StartValue = request.StartValue,
            TargetValue = request.TargetValue,
            CurrentValue = request.CurrentValue,
            Weight = request.Weight,
            Direction = request.Direction,
            AssigneeId = request.AssigneeId
        };
        keyResult.RecalculateProgress();

        await _unitOfWork.Repository<KeyResult>().AddAsync(keyResult, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RecalculateObjectiveAsync(request.ObjectiveId, cancellationToken);
        return MapKeyResult(keyResult);
    }

    public async Task<KpiDto> CreateKpiAsync(CreateKpiRequest request, CancellationToken cancellationToken = default)
    {
        var kpi = new Kpi
        {
            CompanyId = GetCompanyId(),
            Name = request.Name.Trim(),
            Description = request.Description,
            PeriodId = request.PeriodId,
            DepartmentId = request.DepartmentId,
            AssigneeId = request.AssigneeId,
            MetricType = request.MetricType,
            Unit = request.Unit,
            StartValue = request.StartValue,
            TargetValue = request.TargetValue,
            CurrentValue = request.CurrentValue,
            Weight = request.Weight,
            Frequency = request.Frequency,
            Direction = request.Direction,
            CreatedBy = _currentUserService.UserId
        };
        kpi.ApplyApprovedCheckIn(request.CurrentValue);

        await _unitOfWork.Repository<Kpi>().AddAsync(kpi, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapKpi(kpi);
    }

    public async Task<KpiCheckInDto> CreateCheckInAsync(CreateKpiCheckInRequest request, CancellationToken cancellationToken = default)
    {
        var kpi = await _unitOfWork.Repository<Kpi>().GetByIdAsync(request.KpiId, cancellationToken)
            ?? throw new NotFoundException("KPI not found.");
        var alreadyCheckedIn = _unitOfWork.Repository<KpiCheckIn>().Query()
            .Any(x => x.KpiId == request.KpiId && x.CheckInDate >= request.CheckInDate.AddDays(-6) && x.CheckInDate <= request.CheckInDate);
        if (alreadyCheckedIn)
        {
            throw new BusinessRuleException("Only one check-in per KPI is allowed in a 7-day window.");
        }

        var checkIn = new KpiCheckIn
        {
            KpiId = request.KpiId,
            CheckInDate = request.CheckInDate,
            PreviousValue = kpi.CurrentValue,
            NewValue = request.NewValue,
            Progress = PerformanceRules.CalculateProgress(kpi.StartValue, kpi.TargetValue, request.NewValue, kpi.Direction),
            Note = request.Note,
            SubmittedBy = _currentUserService.UserId
        };

        await _unitOfWork.Repository<KpiCheckIn>().AddAsync(checkIn, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapCheckIn(checkIn);
    }

    public async Task<KpiCheckInDto> ApproveCheckInAsync(Guid id, ReviewCheckInRequest request, CancellationToken cancellationToken = default)
    {
        var checkIn = await _unitOfWork.Repository<KpiCheckIn>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Check-in not found.");
        var kpi = await _unitOfWork.Repository<Kpi>().GetByIdAsync(checkIn.KpiId, cancellationToken)
            ?? throw new NotFoundException("KPI not found.");

        checkIn.Status = CheckInStatus.Approved;
        checkIn.ReviewedAt = DateTime.UtcNow;
        checkIn.ReviewedBy = _currentUserService.UserId;
        checkIn.ReviewComment = request.Comment;
        kpi.ApplyApprovedCheckIn(checkIn.NewValue);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapCheckIn(checkIn);
    }

    public async Task<KpiCheckInDto> RejectCheckInAsync(Guid id, ReviewCheckInRequest request, CancellationToken cancellationToken = default)
    {
        var checkIn = await _unitOfWork.Repository<KpiCheckIn>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Check-in not found.");
        checkIn.Status = CheckInStatus.Rejected;
        checkIn.ReviewedAt = DateTime.UtcNow;
        checkIn.ReviewedBy = _currentUserService.UserId;
        checkIn.ReviewComment = request.Comment;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapCheckIn(checkIn);
    }

    private async Task RecalculateObjectiveAsync(Guid objectiveId, CancellationToken cancellationToken)
    {
        var objective = await _unitOfWork.Repository<Objective>().GetByIdAsync(objectiveId, cancellationToken);
        if (objective is null)
        {
            return;
        }

        var keyResults = _unitOfWork.Repository<KeyResult>().Query().Where(x => x.ObjectiveId == objectiveId).ToList();
        objective.Progress = PerformanceRules.WeightedAverage(keyResults.Select(x => (x.Progress, x.Weight)));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private Guid GetCompanyId()
    {
        return _unitOfWork.Repository<Company>().Query().Select(x => x.Id).FirstOrDefault() is { } id && id != Guid.Empty
            ? id
            : throw new BusinessRuleException("Company seed data is missing.");
    }

    private static EvaluationPeriodDto MapPeriod(EvaluationPeriod period) => new(period.Id, period.Name, period.Type, period.StartDate, period.EndDate, period.Status);
    private static ObjectiveDto MapObjective(Objective objective) => new(objective.Id, objective.Title, objective.PeriodId, objective.OwnerType, objective.DepartmentId, objective.OwnerId, objective.Progress, objective.Status);
    private static KeyResultDto MapKeyResult(KeyResult keyResult) => new(keyResult.Id, keyResult.ObjectiveId, keyResult.Title, keyResult.MetricType, keyResult.StartValue, keyResult.TargetValue, keyResult.CurrentValue, keyResult.Progress, keyResult.Weight);
    private static KpiDto MapKpi(Kpi kpi) => new(kpi.Id, kpi.Name, kpi.PeriodId, kpi.DepartmentId, kpi.AssigneeId, kpi.MetricType, kpi.TargetValue, kpi.CurrentValue, kpi.Progress, kpi.Weight, kpi.Rating, kpi.Status);
    private static KpiCheckInDto MapCheckIn(KpiCheckIn checkIn) => new(checkIn.Id, checkIn.KpiId, checkIn.CheckInDate, checkIn.PreviousValue, checkIn.NewValue, checkIn.Progress, checkIn.Note, checkIn.Status, checkIn.ReviewComment);
}
