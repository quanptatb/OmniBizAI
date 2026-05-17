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

        if (vm.SelectedMissionIds.Any())
        {
            foreach (var missionId in vm.SelectedMissionIds.Distinct())
            {
                entity.MissionMappings.Add(new OkrMissionMapping
                {
                    TenantId = tid,
                    MissionVisionId = missionId,
                    CreatedByUserId = tenant.UserId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        if (vm.SelectedDepartmentIds.Any())
        {
            foreach (var departmentId in vm.SelectedDepartmentIds.Distinct())
            {
                entity.DepartmentAllocations.Add(new OkrDepartmentAllocation
                {
                    TenantId = tid,
                    OrganizationUnitId = departmentId,
                    CreatedByUserId = tenant.UserId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }



        if (vm.SelectedEmployeeIds.Any())
        {
            foreach (var userId in vm.SelectedEmployeeIds.Distinct())
            {
                entity.EmployeeAllocations.Add(new OkrEmployeeAllocation
                {
                    TenantId = tid,
                    UserId = userId,
                    CreatedByUserId = tenant.UserId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

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
                .ToListAsync(),
            Employees = await db.AppUsers
                .Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
                .ToListAsync()
        };
    }

    public async Task<OkrEditViewModel?> GetEditFormAsync(Guid id)
    {
        var o = await db.OkrObjectives.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && !x.IsDeleted);
        if (o is null) return null;
        return new OkrEditViewModel { Id = o.Id, ObjectiveName = o.ObjectiveName, Level = o.Level, Cycle = o.Cycle };
    }

    public async Task<bool> UpdateAsync(OkrEditViewModel vm)
    {
        var o = await db.OkrObjectives.FindAsync(vm.Id);
        if (o is null || o.TenantId != tenant.TenantId) return false;
        o.ObjectiveName = vm.ObjectiveName; o.Level = vm.Level; o.Cycle = vm.Cycle;
        o.UpdatedAt = DateTimeOffset.UtcNow; o.UpdatedByUserId = tenant.UserId;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        var o = await db.OkrObjectives.FindAsync(id);
        if (o is null || o.TenantId != tenant.TenantId) return false;
        o.Status = OkrStatus.Active; o.IsActive = true; o.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CloseAsync(Guid id)
    {
        var o = await db.OkrObjectives.FindAsync(id);
        if (o is null || o.TenantId != tenant.TenantId) return false;
        o.Status = OkrStatus.Cancelled; o.IsActive = false; o.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> UpdateKeyResultAsync(UpdateKrProgressViewModel vm)
    {
        var kr = await db.OkrKeyResults.FindAsync(vm.KeyResultId);
        if (kr is null || kr.TenantId != tenant.TenantId) return false;
        kr.CurrentValue = vm.CurrentValue; kr.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> DeleteOkrAsync(Guid id)
    {
        var okr = await db.OkrObjectives.FindAsync(id);
        if (okr is null || okr.TenantId != tenant.TenantId) return false;
        okr.IsDeleted = true; okr.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Delete", EntityName = "OkrObjective", EntityId = id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
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

        if (vm.OkrKeyResultId.HasValue && !vm.OkrObjectiveId.HasValue)
            throw new InvalidOperationException("Vui lòng chọn OKR Objective trước khi chọn Key Result.");

        if (vm.OkrKeyResultId.HasValue && vm.OkrObjectiveId.HasValue)
        {
            var linked = await db.OkrKeyResults.AnyAsync(kr => kr.Id == vm.OkrKeyResultId && kr.OkrObjectiveId == vm.OkrObjectiveId && !kr.IsDeleted && kr.TenantId == tid);
            if (!linked) throw new InvalidOperationException("Key Result không thuộc OKR đã chọn.");
        }

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
            OkrKeyResults = await db.OkrKeyResults
                .Where(kr => kr.TenantId == tid && !kr.IsDeleted)
                .OrderBy(kr => kr.KeyResultName)
                .Select(kr => new SelectOption { Value = kr.Id.ToString(), Text = kr.KeyResultName }).ToListAsync(),
            Periods = await db.EvaluationPeriods
                .Where(p => p.TenantId == tid && !p.IsDeleted && p.Status != EvaluationPeriodStatus.Closed)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.PeriodName }).ToListAsync()
        };
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        var k = await db.KpiDefinitions.FindAsync(id);
        if (k is null || k.TenantId != tenant.TenantId) return false;
        k.Status = KpiStatus.Active; k.IsActive = true; k.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CloseAsync(Guid id)
    {
        var k = await db.KpiDefinitions.FindAsync(id);
        if (k is null || k.TenantId != tenant.TenantId) return false;
        k.Status = KpiStatus.Cancelled; k.IsActive = false; k.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
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
            AvailableTargets = await db.KpiTargets
                .Where(t => t.TenantId == tid && !t.IsDeleted && t.KpiDefinition != null && !t.KpiDefinition.IsDeleted && t.KpiDefinition.Status == KpiStatus.Active)
                .OrderBy(t => t.KpiDefinition!.Code)
                .Select(t => new SelectOption { Value = t.Id.ToString(), Text = $"{t.KpiDefinition!.Code} - {t.KpiDefinition.Name}" })
                .ToListAsync(),
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

    public async Task<EvaluationCreateViewModel> GetCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new EvaluationCreateViewModel
        {
            Users = await db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
                .OrderBy(u => u.FullName).Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName }).ToListAsync(),
            Periods = await db.EvaluationPeriods.Where(p => p.TenantId == tid && !p.IsDeleted)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.PeriodName }).ToListAsync()
        };
    }

    public async Task<Guid> CreateAsync(EvaluationCreateViewModel vm)
    {
        var entity = new EvaluationResult
        {
            TenantId = tenant.TenantId,
            UserId = vm.UserId,
            EvaluationPeriodId = vm.EvaluationPeriodId,
            TotalScore = vm.TotalScore,
            Classification = vm.Classification,
            SubmissionStatus = EvaluationSubmissionStatus.Draft,
            CreatedByUserId = tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.EvaluationResults.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
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

    public async Task<MissionVisionEditViewModel?> GetEditFormAsync(Guid id)
    {
        var m = await db.MissionVisions.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && !x.IsDeleted);
        if (m is null) return null;
        return new MissionVisionEditViewModel { Id = m.Id, Type = m.Type, TargetYear = m.TargetYear, Content = m.Content, FinancialTarget = m.FinancialTarget };
    }

    public async Task<bool> UpdateAsync(MissionVisionEditViewModel vm)
    {
        var m = await db.MissionVisions.FindAsync(vm.Id);
        if (m is null || m.TenantId != tenant.TenantId) return false;
        m.Type = vm.Type; m.TargetYear = vm.TargetYear; m.Content = vm.Content; m.FinancialTarget = vm.FinancialTarget;
        m.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> ToggleAsync(Guid id)
    {
        var m = await db.MissionVisions.FindAsync(id);
        if (m is null || m.TenantId != tenant.TenantId) return false;
        m.IsActive = !m.IsActive; m.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); return true;
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

// ── KPI/OKR Dashboard Service ────────────────────────────────────────────────
public class KpiOkrDashboardService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<KpiOkrDashboardViewModel> GetDashboardAsync()
    {
        var tid = tenant.TenantId;
        var okrs = await db.OkrObjectives.Where(o => o.TenantId == tid && !o.IsDeleted)
            .Include(o => o.KeyResults.Where(kr => !kr.IsDeleted)).ToListAsync();
        var kpis = await db.KpiDefinitions.Where(k => k.TenantId == tid && !k.IsDeleted)
            .Include(k => k.OrganizationUnit).Include(k => k.EvaluationPeriod)
            .OrderByDescending(k => k.CreatedAt).ToListAsync();
        var pendingCi = await db.KpiCheckIns.CountAsync(c => c.TenantId == tid && !c.IsDeleted && c.ReviewStatus == CheckInReviewStatus.Pending);
        var evalCount = await db.EvaluationResults.CountAsync(e => e.TenantId == tid && !e.IsDeleted);

        var avgProgress = okrs.Any() ? Math.Round((decimal)okrs.Average(o =>
            o.KeyResults.Any() ? (double)o.KeyResults.Average(kr =>
                kr.TargetValue == 0 ? 0 :
                kr.IsInverse ? (double)((kr.TargetValue - kr.CurrentValue) / kr.TargetValue * 100) :
                (double)(kr.CurrentValue / kr.TargetValue * 100)) : 0), 1) : 0;

        return new KpiOkrDashboardViewModel
        {
            TotalOkr = okrs.Count,
            ActiveOkr = okrs.Count(o => o.Status == OkrStatus.Active),
            CompletedOkr = okrs.Count(o => o.Status == OkrStatus.Completed),
            TotalKpi = kpis.Count,
            ActiveKpi = kpis.Count(k => k.Status == KpiStatus.Active),
            PendingCheckIns = pendingCi,
            TotalEvaluations = evalCount,
            AvgOkrProgress = avgProgress,
            RecentOkrs = okrs.OrderByDescending(o => o.CreatedAt).Take(5).Select(o => new OkrListItem
            {
                Id = o.Id, ObjectiveName = o.ObjectiveName, Level = o.Level.ToString(),
                Status = o.Status.ToString(), KeyResultCount = o.KeyResults.Count,
                Progress = o.KeyResults.Any() ? Math.Round(o.KeyResults.Average(kr =>
                    kr.TargetValue == 0 ? 0 : kr.CurrentValue / kr.TargetValue * 100), 1) : 0
            }).ToList(),
            RecentKpis = kpis.Take(5).Select(k => new KpiFullListItem
            {
                Id = k.Id, Code = k.Code, Name = k.Name, Unit = k.Unit,
                Status = k.Status.ToString(), Department = k.OrganizationUnit?.Name ?? "",
                PeriodName = k.EvaluationPeriod?.PeriodName
            }).ToList()
        };
    }
}

