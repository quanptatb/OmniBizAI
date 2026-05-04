using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.ViewModels.Kpi;

namespace OmniBizAI.Services.Kpi;

public class KpiService : IKpiService
{
    private readonly ApplicationDbContext _context;

    public KpiService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ObjectiveDto> CreateObjectiveAsync(CreateObjectiveRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var objective = new Objective
        {
            Name = request.Name,
            EvaluationPeriodId = request.EvaluationPeriodId,
            Status = "Draft",
            Progress = 0,
            CreatedBy = userId
        };

        _context.Objectives.Add(objective);
        await _context.SaveChangesAsync(cancellationToken);

        return new ObjectiveDto
        {
            Id = objective.Id,
            Name = objective.Name,
            Status = objective.Status,
            Progress = objective.Progress,
            EvaluationPeriodId = objective.EvaluationPeriodId
        };
    }

    public Task<KeyResultDto> CreateKeyResultAsync(CreateKeyResultRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<KpiDto> CreateKpiAsync(CreateKpiRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var kpi = new OmniBizAI.Models.Entities.Kpi
        {
            Name = request.Name,
            TargetValue = request.TargetValue,
            Direction = request.Direction,
            EvaluationPeriodId = request.EvaluationPeriodId,
            OwnerId = request.OwnerId,
            CurrentValue = 0,
            Progress = 0,
            Status = "Draft",
            CreatedBy = userId
        };

        _context.Kpis.Add(kpi);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(kpi);
    }

    public async Task<CheckInDto> SubmitCheckInAsync(SubmitCheckInRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var kpi = await _context.Kpis.FirstOrDefaultAsync(k => k.Id == request.KpiId, cancellationToken);
        if (kpi == null) throw new Exception("KPI not found");

        var checkIn = new KpiCheckIn
        {
            KpiId = request.KpiId,
            PreviousValue = kpi.CurrentValue,
            NewValue = request.NewValue,
            Comment = request.Comment,
            Status = "Submitted",
            CreatedBy = userId
        };

        _context.KpiCheckIns.Add(checkIn);
        // Tiến độ KPI KHÔNG thay đổi ở đây theo yêu cầu
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(checkIn);
    }

    public async Task<CheckInDto> ApproveCheckInAsync(Guid checkInId, ReviewCheckInRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var checkIn = await _context.KpiCheckIns
            .Include(c => c.Kpi)
            .FirstOrDefaultAsync(c => c.Id == checkInId, cancellationToken);

        if (checkIn == null) throw new Exception("Check-in not found");
        if (checkIn.Status != "Submitted") throw new Exception("Check-in is not in Submitted state");

        checkIn.Status = "Approved";
        checkIn.UpdatedBy = userId;
        checkIn.UpdatedAt = DateTime.UtcNow;

        // Cập nhật giá trị và tiến độ KPI
        var kpi = checkIn.Kpi;
        kpi.CurrentValue = checkIn.NewValue;
        
        if (kpi.TargetValue != 0)
        {
            kpi.Progress = (kpi.CurrentValue / kpi.TargetValue) * 100;
        }

        kpi.UpdatedBy = userId;
        kpi.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(checkIn);
    }

    public async Task<CheckInDto> RejectCheckInAsync(Guid checkInId, ReviewCheckInRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var checkIn = await _context.KpiCheckIns.FirstOrDefaultAsync(c => c.Id == checkInId, cancellationToken);

        if (checkIn == null) throw new Exception("Check-in not found");
        if (checkIn.Status != "Submitted") throw new Exception("Check-in is not in Submitted state");
        if (string.IsNullOrWhiteSpace(request.Comment)) throw new Exception("Comment is required when rejecting");

        checkIn.Status = "Rejected";
        checkIn.Comment = request.Comment;
        checkIn.UpdatedBy = userId;
        checkIn.UpdatedAt = DateTime.UtcNow;

        // Tiến độ KPI KHÔNG thay đổi ở đây
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(checkIn);
    }

    public async Task<EmployeeScorecardDto> GetScorecardAsync(Guid employeeId, Guid periodId, Guid userId, CancellationToken cancellationToken = default)
    {
        var kpis = await _context.Kpis
            .Where(k => k.OwnerId == employeeId && k.EvaluationPeriodId == periodId && !k.IsDeleted)
            .ToListAsync(cancellationToken);

        var scorecard = new EmployeeScorecardDto
        {
            EmployeeId = employeeId,
            EvaluationPeriodId = periodId,
            Kpis = kpis.Select(MapToDto).ToList()
        };

        if (scorecard.Kpis.Any())
        {
            scorecard.OverallProgress = scorecard.Kpis.Average(k => k.Progress);
        }

        return scorecard;
    }

    private KpiDto MapToDto(OmniBizAI.Models.Entities.Kpi kpi)
    {
        return new KpiDto
        {
            Id = kpi.Id,
            Name = kpi.Name,
            TargetValue = kpi.TargetValue,
            CurrentValue = kpi.CurrentValue,
            Progress = kpi.Progress,
            Direction = kpi.Direction,
            Status = kpi.Status,
            EvaluationPeriodId = kpi.EvaluationPeriodId,
            OwnerId = kpi.OwnerId
        };
    }

    private CheckInDto MapToDto(KpiCheckIn checkIn)
    {
        return new CheckInDto
        {
            Id = checkIn.Id,
            KpiId = checkIn.KpiId,
            PreviousValue = checkIn.PreviousValue,
            NewValue = checkIn.NewValue,
            Status = checkIn.Status,
            Comment = checkIn.Comment,
            CreatedAt = checkIn.CreatedAt
        };
    }
}
