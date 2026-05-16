using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

// ── OKR Service ──────────────────────────────────────────────────────────────
public class OkrService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<OkrListViewModel> GetListAsync(string? search, string? level, string? status)
    {
        var tid = tenant.TenantId;
        var q = db.OkrObjectives.Where(o => o.TenantId == tid && !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(o => o.ObjectiveName.Contains(search));
        if (!string.IsNullOrWhiteSpace(level) && Enum.TryParse<OkrLevel>(level, out var lv))
            q = q.Where(o => o.Level == lv);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OkrStatus>(status, out var st))
            q = q.Where(o => o.Status == st);

        var items = await q.Include(o => o.KeyResults.Where(kr => !kr.IsDeleted))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OkrListItem
            {
                Id = o.Id,
                ObjectiveName = o.ObjectiveName,
                Level = o.Level.ToString(),
                Cycle = o.Cycle ?? "",
                Status = o.Status.ToString(),
                IsActive = o.IsActive,
                KeyResultCount = o.KeyResults.Count(kr => !kr.IsDeleted),
                Progress = o.KeyResults.Any(kr => !kr.IsDeleted)
                    ? Math.Round(o.KeyResults.Where(kr => !kr.IsDeleted).Average(kr =>
                        kr.TargetValue == 0 ? 0 :
                        kr.IsInverse ? (kr.TargetValue - kr.CurrentValue) / kr.TargetValue * 100 :
                        kr.CurrentValue / kr.TargetValue * 100), 1)
                    : 0,
                CreatedAt = o.CreatedAt
            }).ToListAsync();

        return new OkrListViewModel
        {
            Items = items,
            SearchTerm = search,
            LevelFilter = level,
            StatusFilter = status
        };
    }

    public async Task<OkrDetailViewModel?> GetDetailAsync(Guid id)
    {
        var o = await db.OkrObjectives.AsNoTracking()
            .Include(x => x.KeyResults.Where(kr => !kr.IsDeleted))
            .Include(x => x.MissionMappings).ThenInclude(m => m.MissionVision)
            .Include(x => x.DepartmentAllocations).ThenInclude(d => d.OrganizationUnit)
            .Include(x => x.EmployeeAllocations).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && !x.IsDeleted);

        if (o is null) return null;

        return new OkrDetailViewModel
        {
            Id = o.Id,
            ObjectiveName = o.ObjectiveName,
            Level = o.Level.ToString(),
            Cycle = o.Cycle ?? "",
            Status = o.Status.ToString(),
            IsActive = o.IsActive,
            CreatedAt = o.CreatedAt,
            KeyResults = o.KeyResults.Select(kr => new OkrKeyResultItem
            {
                Id = kr.Id,
                KeyResultName = kr.KeyResultName,
                Unit = kr.Unit ?? "",
                TargetValue = kr.TargetValue,
                CurrentValue = kr.CurrentValue,
                IsInverse = kr.IsInverse,
                Progress = kr.Progress
            }).ToList(),
            MissionLinks = o.MissionMappings.Where(m => m.MissionVision != null)
                .Select(m => m.MissionVision!.Content ?? "").ToList(),
            DepartmentLinks = o.DepartmentAllocations.Where(d => d.OrganizationUnit != null)
                .Select(d => d.OrganizationUnit!.Name).ToList(),
            EmployeeLinks = o.EmployeeAllocations.Where(e => e.User != null)
                .Select(e => e.User!.FullName).ToList()
        };
    }

    public async Task<Guid> CreateAsync(OkrCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var entity = new OkrObjective
        {
            TenantId = tid,
            ObjectiveName = vm.ObjectiveName.Trim(),
            Level = vm.Level,
            Cycle = vm.Cycle,
            Status = OkrStatus.Draft,
            IsActive = true,
            CreatedByUserId = tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (vm.KeyResults?.Any() == true)
        {
            foreach (var kr in vm.KeyResults)
            {
                entity.KeyResults.Add(new OkrKeyResult
                {
                    TenantId = tid,
                    KeyResultName = kr.KeyResultName.Trim(),
                    Unit = kr.Unit,
                    TargetValue = kr.TargetValue,
                    IsInverse = kr.IsInverse,
                    CreatedByUserId = tenant.UserId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        db.OkrObjectives.Add(entity);
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName,
            Action = "Create", EntityName = "OkrObjective", EntityId = entity.Id,
            NewValuesJson = $"{{\"ObjectiveName\":\"{entity.ObjectiveName}\",\"Level\":\"{entity.Level}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<OkrCreateViewModel> GetCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new OkrCreateViewModel
        {
            Departments = await db.OrganizationUnits
                .Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
                .ToListAsync(),
            Missions = await db.MissionVisions
                .Where(m => m.TenantId == tid && m.IsActive && !m.IsDeleted)
                .Select(m => new SelectOption { Value = m.Id.ToString(), Text = m.Content ?? "" })
                .ToListAsync()
        };
    }
}

// ── KPI Management Service ───────────────────────────────────────────────────
public class KpiManagementService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<KpiFullListViewModel> GetListAsync(string? search, string? status, string? periodId, string? ownerType)
    {
        var tid = tenant.TenantId;
        var q = db.KpiDefinitions.Where(k => k.TenantId == tid && !k.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(k => k.Name.Contains(search) || k.Code.Contains(search));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<KpiStatus>(status, out var st))
            q = q.Where(k => k.Status == st);
        if (!string.IsNullOrWhiteSpace(periodId) && Guid.TryParse(periodId, out var pid))
            q = q.Where(k => k.EvaluationPeriodId == pid);
        if (!string.IsNullOrWhiteSpace(ownerType) && Enum.TryParse<KpiOwnerType>(ownerType, out var ot))
            q = q.Where(k => k.OwnerType == ot);

        var items = await q
            .Include(k => k.OrganizationUnit)
            .Include(k => k.OkrObjective)
            .Include(k => k.EvaluationPeriod)
            .Include(k => k.Targets.Where(t => !t.IsDeleted))
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new KpiFullListItem
            {
                Id = k.Id,
                Code = k.Code,
                Name = k.Name,
                Unit = k.Unit,
                OwnerType = k.OwnerType.ToString(),
                MeasureType = k.MeasureType.ToString(),
                PropertyType = k.PropertyType.ToString(),
                Status = k.Status.ToString(),
                Department = k.OrganizationUnit != null ? k.OrganizationUnit.Name : "",
                OkrName = k.OkrObjective != null ? k.OkrObjective.ObjectiveName : null,
                PeriodName = k.EvaluationPeriod != null ? k.EvaluationPeriod.PeriodName : null,
                TargetValue = k.Targets.Any() ? k.Targets.First().TargetValue : 0,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt
            }).ToListAsync();

        return new KpiFullListViewModel
        {
            Items = items,
            SearchTerm = search,
            StatusFilter = status,
            PeriodFilter = periodId,
            OwnerTypeFilter = ownerType,
            Periods = await db.EvaluationPeriods
                .Where(p => p.TenantId == tid && !p.IsDeleted)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.PeriodName })
                .ToListAsync()
        };
    }

    public async Task<KpiDetailViewModel?> GetDetailAsync(Guid id)
    {
        var k = await db.KpiDefinitions.AsNoTracking()
            .Include(x => x.OrganizationUnit)
            .Include(x => x.OkrObjective)
            .Include(x => x.OkrKeyResult)
            .Include(x => x.EvaluationPeriod)
            .Include(x => x.AssignerUser)
            .Include(x => x.Targets.Where(t => !t.IsDeleted))
            .Include(x => x.DepartmentAssignments.Where(d => !d.IsDeleted)).ThenInclude(d => d.OrganizationUnit)
            .Include(x => x.EmployeeAssignments.Where(e => !e.IsDeleted)).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && !x.IsDeleted);

        if (k is null) return null;

        return new KpiDetailViewModel
        {
            Id = k.Id,
            Code = k.Code,
            Name = k.Name,
            Description = k.Description,
            Unit = k.Unit,
            OwnerType = k.OwnerType.ToString(),
            MeasureType = k.MeasureType.ToString(),
            PropertyType = k.PropertyType.ToString(),
            Status = k.Status.ToString(),
            IsActive = k.IsActive,
            Department = k.OrganizationUnit?.Name,
            OkrName = k.OkrObjective?.ObjectiveName,
            KeyResultName = k.OkrKeyResult?.KeyResultName,
            PeriodName = k.EvaluationPeriod?.PeriodName,
            AssignerName = k.AssignerUser?.FullName,
            CreatedAt = k.CreatedAt,
            Targets = k.Targets.Select(t => new KpiTargetItem
            {
                Id = t.Id,
                TargetValue = t.TargetValue,
                PassThreshold = t.PassThreshold,
                FailThreshold = t.FailThreshold,
                PeriodStart = t.PeriodStart,
                PeriodEnd = t.PeriodEnd,
                CheckInFrequencyDays = t.CheckInFrequencyDays,
                ReminderEnabled = t.ReminderEnabled
            }).ToList(),
            DepartmentAssignments = k.DepartmentAssignments
                .Select(d => d.OrganizationUnit?.Name ?? "").ToList(),
            EmployeeAssignments = k.EmployeeAssignments
                .Select(e => new KpiEmployeeAssignmentItem
                {
                    UserName = e.User?.FullName ?? "",
                    Weight = e.Weight
                }).ToList()
        };
    }

    public async Task<Guid> CreateAsync(KpiCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var count = await db.KpiDefinitions.CountAsync(k => k.TenantId == tid);

        var entity = new KpiDefinition
        {
            TenantId = tid,
            Code = Helpers.CodeGeneratorHelper.GenerateKpiCode(count + 1),
            Name = vm.Name.Trim(),
            Description = vm.Description?.Trim(),
            Unit = vm.Unit.Trim(),
            OwnerType = vm.OwnerType,
            PeriodType = vm.PeriodType,
            MeasureType = vm.MeasureType,
            PropertyType = vm.PropertyType,
            OrganizationUnitId = vm.OrganizationUnitId,
            OkrObjectiveId = vm.OkrObjectiveId,
            OkrKeyResultId = vm.OkrKeyResultId,
            EvaluationPeriodId = vm.EvaluationPeriodId,
            AssignerUserId = tenant.UserId,
            Status = KpiStatus.Draft,
            IsActive = true,
            CreatedByUserId = tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Add target
        if (vm.TargetValue > 0)
        {
            entity.Targets.Add(new KpiTarget
            {
                TenantId = tid,
                TargetValue = vm.TargetValue,
                PassThreshold = vm.PassThreshold,
                FailThreshold = vm.FailThreshold,
                PeriodStart = vm.PeriodStart ?? DateOnly.FromDateTime(DateTime.Today),
                PeriodEnd = vm.PeriodEnd ?? DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
                CheckInFrequencyDays = vm.CheckInFrequencyDays,
                DeadlineTime = vm.DeadlineTime,
                ReminderEnabled = vm.ReminderEnabled,
                CreatedByUserId = tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        db.KpiDefinitions.Add(entity);
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName,
            Action = "Create", EntityName = "KpiDefinition", EntityId = entity.Id,
            NewValuesJson = $"{{\"Code\":\"{entity.Code}\",\"Name\":\"{entity.Name}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<KpiCreateViewModel> GetCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new KpiCreateViewModel
        {
            Departments = await db.OrganizationUnits
                .Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync(),
            OkrObjectives = await db.OkrObjectives
                .Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted && o.Status == OkrStatus.Active)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.ObjectiveName }).ToListAsync(),
            Periods = await db.EvaluationPeriods
                .Where(p => p.TenantId == tid && !p.IsDeleted && p.Status != EvaluationPeriodStatus.Closed)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.PeriodName }).ToListAsync()
        };
    }
}

// ── KPI Check-In Service ─────────────────────────────────────────────────────
public class KpiCheckInService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<KpiCheckInListViewModel> GetListAsync(string? search, string? reviewStatus, int page = 1)
    {
        var tid = tenant.TenantId;
        var q = db.KpiCheckIns.Where(c => c.TenantId == tid && !c.IsDeleted)
            .Include(c => c.KpiTarget).ThenInclude(t => t!.KpiDefinition)
            .Include(c => c.User);

        IQueryable<KpiCheckIn> filtered = q;

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(c =>
                (c.KpiTarget != null && c.KpiTarget.KpiDefinition != null && c.KpiTarget.KpiDefinition.Name.Contains(search)) ||
                (c.User != null && c.User.FullName.Contains(search)));

        if (!string.IsNullOrWhiteSpace(reviewStatus) && Enum.TryParse<CheckInReviewStatus>(reviewStatus, out var rs))
            filtered = filtered.Where(c => c.ReviewStatus == rs);

        var total = await filtered.CountAsync();
        var items = await filtered
            .OrderByDescending(c => c.CheckInDate).ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * 20).Take(20)
            .Select(c => new KpiCheckInListItem
            {
                Id = c.Id,
                KpiName = c.KpiTarget != null && c.KpiTarget.KpiDefinition != null ? c.KpiTarget.KpiDefinition.Name : "",
                KpiCode = c.KpiTarget != null && c.KpiTarget.KpiDefinition != null ? c.KpiTarget.KpiDefinition.Code : "",
                UserName = c.User != null ? c.User.FullName : "",
                CheckInDate = c.CheckInDate,
                ProgressValue = c.ProgressValue,
                ReviewStatus = c.ReviewStatus.ToString(),
                IsLate = c.IsLate,
                ReviewScore = c.ReviewScore
            }).ToListAsync();

        return new KpiCheckInListViewModel
        {
            Items = items,
            TotalCount = total,
            Page = page,
            SearchTerm = search,
            ReviewStatusFilter = reviewStatus
        };
    }

    public async Task<(bool Success, string Message)> SubmitCheckInAsync(KpiCheckInSubmitViewModel vm)
    {
        var tid = tenant.TenantId;
        var target = await db.KpiTargets
            .Include(t => t.KpiDefinition)
            .FirstOrDefaultAsync(t => t.Id == vm.KpiTargetId && t.TenantId == tid && !t.IsDeleted);

        if (target is null)
            return (false, "Không tìm thấy chỉ tiêu KPI.");

        if (target.KpiDefinition?.Status != KpiStatus.Active)
            return (false, "KPI chưa được kích hoạt.");

        var checkIn = new KpiCheckIn
        {
            TenantId = tid,
            KpiTargetId = target.Id,
            UserId = tenant.UserId,
            SubmittedByUserId = tenant.UserId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today),
            ProgressValue = vm.ProgressValue,
            Comment = vm.Comment?.Trim(),
            ReviewStatus = CheckInReviewStatus.Pending,
            CreatedByUserId = tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Check if late
        if (target.CheckInFrequencyDays.HasValue && target.DeadlineTime.HasValue)
        {
            var deadline = Helpers.KpiCheckInScheduleHelper.GetNextDeadlineWithTime(
                checkIn.CheckInDate.AddDays(-target.CheckInFrequencyDays.Value),
                target.CheckInFrequencyDays.Value,
                target.DeadlineTime);
            checkIn.DeadlineAt = deadline;
            checkIn.IsLate = Helpers.KpiCheckInScheduleHelper.IsOverdue(deadline);
        }

        db.KpiCheckIns.Add(checkIn);
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName,
            Action = "CheckIn", EntityName = "KpiCheckIn", EntityId = checkIn.Id,
            NewValuesJson = $"{{\"ProgressValue\":{vm.ProgressValue}}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
        return (true, "Đã ghi nhận check-in thành công.");
    }

    public async Task<(bool Success, string Message)> ReviewCheckInAsync(Guid checkInId, CheckInReviewStatus decision, string? comment, decimal? score)
    {
        var checkIn = await db.KpiCheckIns
            .FirstOrDefaultAsync(c => c.Id == checkInId && c.TenantId == tenant.TenantId && !c.IsDeleted);

        if (checkIn is null) return (false, "Không tìm thấy check-in.");
        if (checkIn.ReviewStatus != CheckInReviewStatus.Pending) return (false, "Check-in đã được review.");

        checkIn.ReviewStatus = decision;
        checkIn.ReviewedByUserId = tenant.UserId;
        checkIn.ReviewedAt = DateTimeOffset.UtcNow;
        checkIn.ReviewComment = comment?.Trim();
        checkIn.ReviewScore = score;
        checkIn.UpdatedAt = DateTimeOffset.UtcNow;
        checkIn.UpdatedByUserId = tenant.UserId;

        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName,
            Action = "ReviewCheckIn", EntityName = "KpiCheckIn", EntityId = checkIn.Id,
            NewValuesJson = $"{{\"ReviewStatus\":\"{decision}\",\"Score\":{score}}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
        return (true, $"Đã {(decision == CheckInReviewStatus.Approved ? "duyệt" : "từ chối")} check-in.");
    }
}

// ── Evaluation Service ───────────────────────────────────────────────────────
public class EvaluationService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<EvaluationListViewModel> GetListAsync(string? periodId)
    {
        var tid = tenant.TenantId;
        IQueryable<EvaluationResult> q = db.EvaluationResults.Where(e => e.TenantId == tid && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(periodId) && Guid.TryParse(periodId, out var pid))
            q = q.Where(e => e.EvaluationPeriodId == pid);

        var items = await q
            .Include(e => e.User)
            .Include(e => e.EvaluationPeriod)
            .Include(e => e.GradingRank)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EvaluationListItem
            {
                Id = e.Id,
                UserName = e.User != null ? e.User.FullName : "",
                PeriodName = e.EvaluationPeriod != null ? e.EvaluationPeriod.PeriodName : "",
                TotalScore = e.TotalScore,
                RankName = e.GradingRank != null ? e.GradingRank.RankName : null,
                Classification = e.Classification,
                SubmissionStatus = e.SubmissionStatus.ToString(),
                CreatedAt = e.CreatedAt
            }).ToListAsync();

        return new EvaluationListViewModel
        {
            Items = items,
            PeriodFilter = periodId,
            Periods = await db.EvaluationPeriods
                .Where(p => p.TenantId == tid && !p.IsDeleted)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.PeriodName })
                .ToListAsync()
        };
    }
}

// ── Mission/Vision Service ───────────────────────────────────────────────────
public class MissionVisionService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<MissionVisionListViewModel> GetListAsync()
    {
        var tid = tenant.TenantId;
        var items = await db.MissionVisions
            .Where(m => m.TenantId == tid && !m.IsDeleted)
            .OrderBy(m => m.Type).ThenByDescending(m => m.TargetYear)
            .Select(m => new MissionVisionItem
            {
                Id = m.Id,
                Type = m.Type.ToString(),
                TargetYear = m.TargetYear,
                Content = m.Content ?? "",
                FinancialTarget = m.FinancialTarget,
                IsActive = m.IsActive
            }).ToListAsync();

        return new MissionVisionListViewModel { Items = items };
    }

    public async Task<Guid> CreateAsync(MissionVisionCreateViewModel vm)
    {
        var entity = new MissionVision
        {
            TenantId = tenant.TenantId,
            Type = vm.Type,
            TargetYear = vm.TargetYear,
            Content = vm.Content?.Trim(),
            FinancialTarget = vm.FinancialTarget,
            IsActive = true,
            CreatedByUserId = tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.MissionVisions.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }
}

// ── OKR Progress Service ─────────────────────────────────────────────────────
public class OkrProgressService(ApplicationDbContext db)
{
    /// <summary>Recalculate OKR progress from Key Results and update status.</summary>
    public async Task RecalculateAsync(Guid okrId)
    {
        var okr = await db.OkrObjectives
            .Include(o => o.KeyResults.Where(kr => !kr.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == okrId && !o.IsDeleted);

        if (okr is null || !okr.KeyResults.Any()) return;

        var progress = okr.TotalProgress;

        if (progress >= 100 && okr.Status == OkrStatus.Active)
        {
            okr.Status = OkrStatus.Completed;
            okr.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
    }
}
