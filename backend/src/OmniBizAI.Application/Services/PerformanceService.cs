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

    public Task<PagedResult<EvaluationPeriodDto>> GetPeriodsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<EvaluationPeriod>().Query();
        return Task.FromResult(PagedResult<EvaluationPeriodDto>.Create(q.Select(x => new EvaluationPeriodDto(x.Id, x.Name, x.Type, x.StartDate, x.EndDate, x.Status)), request));
    }
    public async Task<EvaluationPeriodDto> GetPeriodAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<EvaluationPeriod>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new EvaluationPeriodDto(x.Id, x.Name, x.Type, x.StartDate, x.EndDate, x.Status);
    }
    public async Task<EvaluationPeriodDto> UpdatePeriodAsync(Guid id, UpdateEvaluationPeriodRequest request, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<EvaluationPeriod>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        x.Name = request.Name; x.Type = request.Type; x.StartDate = request.StartDate; x.EndDate = request.EndDate; x.Status = request.Status;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetPeriodAsync(id, cancellationToken);
    }

    public Task<PagedResult<ObjectiveDto>> GetObjectivesAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Objective>().Query();
        return Task.FromResult(PagedResult<ObjectiveDto>.Create(q.Select(x => new ObjectiveDto(x.Id, x.Title, x.PeriodId, x.OwnerType, x.DepartmentId, x.OwnerId, x.Progress, x.Status)), request));
    }
    public async Task<ObjectiveDto> GetObjectiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Objective>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new ObjectiveDto(x.Id, x.Title, x.PeriodId, x.OwnerType, x.DepartmentId, x.OwnerId, x.Progress, x.Status);
    }
    public Task<IReadOnlyCollection<ObjectiveDto>> GetObjectiveTreeAsync(CancellationToken cancellationToken = default)
    {
        var all = _unitOfWork.Repository<Objective>().Query().ToList();
        return Task.FromResult<IReadOnlyCollection<ObjectiveDto>>(all.Select(x => new ObjectiveDto(x.Id, x.Title, x.PeriodId, x.OwnerType, x.DepartmentId, x.OwnerId, x.Progress, x.Status)).ToList());
    }
    public async Task<ObjectiveDto> UpdateObjectiveAsync(Guid id, UpdateObjectiveRequest request, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Objective>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        x.Title = request.Title; x.Description = request.Description; x.ParentId = request.ParentId; x.OwnerType = request.OwnerType; x.DepartmentId = request.DepartmentId; x.OwnerId = request.OwnerId; x.DueDate = request.DueDate; x.Status = request.Status;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetObjectiveAsync(id, cancellationToken);
    }
    public async Task DeleteObjectiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Objective>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<Objective>().Remove(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<KeyResultDto>> GetKeyResultsAsync(Guid? objectiveId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<KeyResult>().Query();
        if(objectiveId.HasValue) q = q.Where(x => x.ObjectiveId == objectiveId.Value);
        return Task.FromResult(PagedResult<KeyResultDto>.Create(q.Select(x => new KeyResultDto(x.Id, x.ObjectiveId, x.Title, x.MetricType, x.StartValue, x.TargetValue, x.CurrentValue, x.Progress, x.Weight)), request));
    }
    public async Task<KeyResultDto> UpdateKeyResultAsync(Guid id, UpdateKeyResultRequest request, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<KeyResult>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        x.Title = request.Title; x.MetricType = request.MetricType; x.Unit = request.Unit; x.TargetValue = request.TargetValue; x.Weight = request.Weight; x.Direction = request.Direction; x.AssigneeId = request.AssigneeId;
        x.RecalculateProgress();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RecalculateObjectiveAsync(x.ObjectiveId, cancellationToken);
        return new KeyResultDto(x.Id, x.ObjectiveId, x.Title, x.MetricType, x.StartValue, x.TargetValue, x.CurrentValue, x.Progress, x.Weight);
    }
    public async Task DeleteKeyResultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<KeyResult>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<KeyResult>().Remove(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<KpiDto>> GetKpisAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<Kpi>().Query();
        return Task.FromResult(PagedResult<KpiDto>.Create(q.Select(x => new KpiDto(x.Id, x.Name, x.PeriodId, x.DepartmentId, x.AssigneeId, x.MetricType, x.TargetValue, x.CurrentValue, x.Progress, x.Weight, x.Rating, x.Status)), request));
    }
    public async Task<KpiDto> GetKpiAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Kpi>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        return new KpiDto(x.Id, x.Name, x.PeriodId, x.DepartmentId, x.AssigneeId, x.MetricType, x.TargetValue, x.CurrentValue, x.Progress, x.Weight, x.Rating, x.Status);
    }
    public async Task<KpiDto> UpdateKpiAsync(Guid id, UpdateKpiRequest request, CancellationToken cancellationToken = default)
    {
        var x = await _unitOfWork.Repository<Kpi>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        x.Name = request.Name; x.Description = request.Description; x.DepartmentId = request.DepartmentId; x.AssigneeId = request.AssigneeId; x.MetricType = request.MetricType; x.Unit = request.Unit; x.TargetValue = request.TargetValue; x.Weight = request.Weight; x.Frequency = request.Frequency; x.Direction = request.Direction; x.Status = request.Status;
        await _unitOfWork.SaveChangesAsync(cancellationToken); return await GetKpiAsync(id, cancellationToken);
    }
    public async Task DeleteKpiAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.Repository<Kpi>().GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Not found");
        _unitOfWork.Repository<Kpi>().Remove(e); await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<KpiCheckInDto>> GetCheckInsAsync(Guid? kpiId, string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var q = _unitOfWork.Repository<KpiCheckIn>().Query();
        if (kpiId.HasValue)
        {
            q = q.Where(x => x.KpiId == kpiId.Value);
        }
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CheckInStatus>(status, true, out var parsedStatus))
        {
            q = q.Where(x => x.Status == parsedStatus);
        }
        return Task.FromResult(PagedResult<KpiCheckInDto>.Create(q.Select(x => new KpiCheckInDto(x.Id, x.KpiId, x.CheckInDate, x.PreviousValue, x.NewValue, x.Progress, x.Note, x.Status, x.ReviewComment)), request));
    }
    public Task<KpiScorecardDto> GetScorecardAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = _unitOfWork.Repository<Employee>().Query().FirstOrDefault(x => x.Id == employeeId)
            ?? throw new NotFoundException("Employee not found.");
        var objectives = _unitOfWork.Repository<Objective>().Query()
            .Where(x => x.OwnerId == employeeId || x.DepartmentId == employee.DepartmentId)
            .Select(x => new ObjectiveDto(x.Id, x.Title, x.PeriodId, x.OwnerType, x.DepartmentId, x.OwnerId, x.Progress, x.Status))
            .ToList();
        var kpis = _unitOfWork.Repository<Kpi>().Query()
            .Where(x => x.AssigneeId == employeeId || x.DepartmentId == employee.DepartmentId)
            .Select(x => new KpiDto(x.Id, x.Name, x.PeriodId, x.DepartmentId, x.AssigneeId, x.MetricType, x.TargetValue, x.CurrentValue, x.Progress, x.Weight, x.Rating, x.Status))
            .ToList();
        var weighted = kpis.Count == 0
            ? objectives.Select(x => (Score: x.Progress, Weight: 1m)).ToList()
            : kpis.Select(x => (Score: x.Progress, Weight: x.Weight <= 0 ? 1 : x.Weight)).ToList();
        var totalWeight = weighted.Sum(x => x.Weight);
        var score = totalWeight <= 0 ? 0 : Math.Round(weighted.Sum(x => x.Score * x.Weight) / totalWeight, 2);
        return Task.FromResult(new KpiScorecardDto(employeeId, employee.FullName, score, objectives, kpis));
    }

}
