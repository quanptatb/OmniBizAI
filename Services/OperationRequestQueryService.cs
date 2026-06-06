using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Domain.StateMachines;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class OperationRequestQueryService(ApplicationDbContext db, ITenantContext tenant)
{
    private const string CriticalOverdueFilter = "CriticalOverdue";
    private const string OverBudgetFilter = "OverBudget";
    private const decimal CostOverrunThresholdPercent = 20m;
    private static readonly JsonSerializerOptions TemplateJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private sealed record OperationAssignmentAccess(bool HasAssignments, bool HasPrimary, bool HasSupport, bool HasWatcher);

    public async Task<OperationRequestListViewModel> GetListAsync(string? search, string? status, string? priority, Guid? deptId, int page = 1)
    {
        var tid = tenant.TenantId;
        var baseQ = db.OperationRequests.Where(r => r.TenantId == tid && !r.IsDeleted);

        var draftCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Draft);
        var submittedCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview);
        var inProgressCount = await baseQ.CountAsync(r => r.Status == OperationStatus.InProgress);
        var completedCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Completed);
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var overdueQuery = BuildOverdueQuery(now, today);
        var overdueCount = await baseQ.CountAsync(overdueQuery);
        var criticalCount = await baseQ.CountAsync(r => r.Priority == PriorityLevel.Critical);
        var criticalOverdueCount = await baseQ.Where(r => r.Priority == PriorityLevel.Critical).CountAsync(overdueQuery);
        var overBudgetCount = await baseQ.CountAsync(r => r.CostVariancePercent.HasValue && r.CostVariancePercent > CostOverrunThresholdPercent);

        var q = baseQ;
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(r => r.Title.Contains(search) || r.RequestNo.Contains(search));
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals(CriticalOverdueFilter, StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(r => r.Priority == PriorityLevel.Critical).Where(overdueQuery);
            }
            else if (status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(overdueQuery);
            }
            else if (status.Equals(OverBudgetFilter, StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(r => r.CostVariancePercent.HasValue && r.CostVariancePercent > CostOverrunThresholdPercent);
            }
            else if (Enum.TryParse<OperationStatus>(status, out var st))
            {
                q = q.Where(r => r.Status == st);
            }
        }
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<PriorityLevel>(priority, out var pr)) q = q.Where(r => r.Priority == pr);
        if (deptId.HasValue) q = q.Where(r => r.OrganizationUnitId == deptId.Value);

        var total = await q.CountAsync();
        var maxSlaDueAt = DateTimeOffset.MaxValue;
        var maxDueDate = DateOnly.MaxValue;
        var items = await q
            .OrderByDescending(r => r.Priority == PriorityLevel.Critical ? 4 : r.Priority == PriorityLevel.High ? 3 : r.Priority == PriorityLevel.Normal ? 2 : 1)
            .ThenBy(r => (r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview)
                ? (r.ApprovalDueAt ?? maxSlaDueAt)
                : (r.Status == OperationStatus.Approved || r.Status == OperationStatus.InProgress || r.Status == OperationStatus.OnHold)
                    ? (r.ResolutionDueAt ?? maxSlaDueAt)
                    : maxSlaDueAt)
            .ThenBy(r => r.DueDate ?? maxDueDate)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * 20).Take(20)
            .Join(db.AppUsers, r => r.RequestedByUserId, u => u.Id, (r, u) => new { r, u })
            .Join(db.OrganizationUnits, x => x.r.OrganizationUnitId, o => o.Id, (x, o) => new OperationRequestListItem
            {
                Id = x.r.Id,
                RequestNo = x.r.RequestNo,
                Title = x.r.Title,
                Type = x.r.Type,
                Status = x.r.Status.ToString(),
                Priority = x.r.Priority.ToString(),
                Department = o.Name,
                CreatedBy = x.u.FullName,
                CreatedAt = x.r.CreatedAt,
                DueDate = x.r.DueDate,
                TotalAmount = x.r.TotalAmount,
                EstimatedCost = x.r.EstimatedCost,
                ActualCost = x.r.ActualCost,
                CostVariance = x.r.CostVariance,
                CostVariancePercent = x.r.CostVariancePercent,
                PriorityWeight = x.r.Priority == PriorityLevel.Critical ? 4 : x.r.Priority == PriorityLevel.High ? 3 : x.r.Priority == PriorityLevel.Normal ? 2 : 1,
                ApprovalDueAt = x.r.ApprovalDueAt,
                ResolutionDueAt = x.r.ResolutionDueAt,
                SlaDueAt = OperationSlaService.GetActiveDueAt(x.r.Status, x.r.ApprovalDueAt, x.r.ResolutionDueAt),
                SlaStage = OperationSlaService.GetActiveStage(x.r.Status)
            })
            .ToListAsync();

        return new OperationRequestListViewModel
        {
            Items = items,
            TotalCount = total,
            Page = page,
            DraftCount = draftCount,
            SubmittedCount = submittedCount,
            InProgressCount = inProgressCount,
            CompletedCount = completedCount,
            OverdueCount = overdueCount,
            CriticalCount = criticalCount,
            CriticalOverdueCount = criticalOverdueCount,
            OverBudgetCount = overBudgetCount,
            SearchTerm = search,
            StatusFilter = status,
            PriorityFilter = priority,
            DeptFilter = deptId,
            Departments = await GetDepartmentOptionsAsync(tid)
        };
    }

    public async Task<OperationRequestDetailViewModel?> GetDetailAsync(Guid id)
    {
        var r = await db.OperationRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted);
        if (r is null) return null;

        var creator = await db.AppUsers.FindAsync(r.RequestedByUserId);
        var dept = await db.OrganizationUnits.FindAsync(r.OrganizationUnitId);
        var customer = r.CustomerId.HasValue ? await db.Customers.FindAsync(r.CustomerId.Value) : null;
        var customerSite = r.CustomerSiteId.HasValue ? await db.CustomerSites.FindAsync(r.CustomerSiteId.Value) : null;
        var lines = await db.Set<OperationRequestLine>().Where(l => l.OperationRequestId == id && !l.IsDeleted)
            .Select(l => new OrderLineDisplayItem
            {
                Id = l.Id,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineAmount = l.LineAmount,
                Note = l.Note,
                ProductName = l.ProductService != null ? l.ProductService.Name : null,
                ProductCode = l.ProductService != null ? l.ProductService.Code : null
            }).ToListAsync();
        var approvals = await db.ApprovalTasks.Where(t => t.TargetId == id && !t.IsDeleted)
            .Select(t => new ApprovalTaskItem
            {
                Id = t.Id,
                TargetType = t.TargetType,
                TargetId = t.TargetId,
                StepCode = t.StepCode,
                StepName = t.StepCode == "DEPARTMENT_REVIEW" ? "Trưởng bộ phận duyệt" : "Ban lãnh đạo duyệt",
                Status = t.Status.ToString(),
                AssignedRole = t.AssignedRole,
                DecisionNote = t.DecisionNote,
                DecidedAt = t.DecidedAt
            }).ToListAsync();
        var workItems = await db.WorkItems.Where(w => w.OperationRequestId == id && !w.IsDeleted)
            .Select(w => new WorkItemListItem { Id = w.Id, Title = w.Title, Status = w.Status.ToString(), Priority = w.Priority.ToString(), DueDate = w.DueDate }).ToListAsync();
        var aiInsights = await db.AiInsights.Where(a => a.ContextId == id && a.TenantId == tenant.TenantId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt).Take(3)
            .Select(a => new AiInsightListItem { Id = a.Id, ContextType = a.ContextType, Question = a.Question, Summary = a.Summary, Recommendation = a.Recommendation, RiskLevel = a.RiskLevel.ToString(), Status = a.Status.ToString(), CreatedAt = a.CreatedAt }).ToListAsync();
        var activityLog = await db.AuditLogs
            .Where(a => a.TenantId == tenant.TenantId && a.EntityId == id && (a.EntityName == "OperationRequest" || a.EntityName == "ApprovalTask" || a.EntityName == "WorkItem"))
            .OrderByDescending(a => a.CreatedAt).Take(20)
            .Select(a => new ActivityLogItem { UserName = a.UserName, Action = a.Action, Details = a.NewValuesJson, OccurredAt = a.CreatedAt }).ToListAsync();

        var comments = await GetThreadedCommentsAsync(id);
        var progressLogs = await db.OperationProgressLogs
            .Where(p => p.OperationRequestId == id && p.TenantId == tenant.TenantId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Join(db.AppUsers, p => p.CreatedByUserId!.Value, u => u.Id, (p, u) => new OperationProgressLogItem
            {
                Id = p.Id,
                ProgressPercent = p.ProgressPercent,
                Note = p.Note,
                CreatedByName = u.FullName,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
        var attachments = await db.Attachments
            .AsNoTracking()
            .Where(a => a.TenantId == tenant.TenantId
                && a.EntityName == OperationAttachmentService.OperationRequestEntityName
                && a.EntityId == id
                && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Join(db.AppUsers, a => a.UploadedByUserId, u => u.Id, (a, u) => new OperationAttachmentItem
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                UploadedByName = u.FullName,
                UploadedAt = a.CreatedAt
            })
            .ToListAsync();
        var assignments = await GetAssignmentsAsync(id);

        var assignmentAccess = await GetAssignmentAccessAsync(id);
        var canManageAssignments = CanManageOperationAssignments();
        var canManageWork = IsOperationAdmin()
            || (assignmentAccess.HasAssignments ? assignmentAccess.HasPrimary : HasLegacyOperationManagerRole());
        var canSupportWork = IsOperationAdmin()
            || (assignmentAccess.HasAssignments ? assignmentAccess.HasPrimary || assignmentAccess.HasSupport : HasLegacyOperationContributorRole());
        var canEditDraft = r.Status is OperationStatus.Draft or OperationStatus.Rejected
            && (r.RequestedByUserId == tenant.UserId || canSupportWork);
        var nextStates = OperationRequestStateMachine.NextStates(r.Status);
        var slaDueAt = OperationSlaService.GetActiveDueAt(r.Status, r.ApprovalDueAt, r.ResolutionDueAt);
        var currentProgress = progressLogs.FirstOrDefault()?.ProgressPercent
            ?? (r.Status == OperationStatus.Completed ? 100m : r.Status == OperationStatus.InProgress ? 5m : 0m);
        var progressStart = r.UpdatedAt ?? r.ApprovedAt ?? r.CreatedAt;
        var isProgressStale = r.Status == OperationStatus.InProgress
            && !progressLogs.Any()
            && DateTimeOffset.UtcNow - progressStart > TimeSpan.FromHours(48);
        var linkedPlan = await db.OperationPlans
            .AsNoTracking()
            .Where(p => p.TenantId == tenant.TenantId
                && !p.IsDeleted
                && p.SourceOperationRequestId == r.Id)
            .Select(p => new { p.Id, p.Code })
            .FirstOrDefaultAsync();

        return new OperationRequestDetailViewModel
        {
            Id = r.Id,
            RequestNo = r.RequestNo,
            Title = r.Title,
            Type = r.Type,
            Status = r.Status.ToString(),
            Priority = r.Priority.ToString(),
            Department = dept?.Name ?? "",
            DepartmentId = r.OrganizationUnitId,
            Customer = customer?.Name,
            CreatedBy = creator?.FullName ?? "",
            CreatedAt = r.CreatedAt,
            DueDate = r.DueDate,
            TotalAmount = r.TotalAmount,
            Description = r.Description,
            EstimatedCost = r.EstimatedCost,
            ActualCost = r.ActualCost,
            CostVariance = r.CostVariance,
            CostVariancePercent = r.CostVariancePercent,
            CostVarianceCalculatedAt = r.CostVarianceCalculatedAt,
            CustomerSiteName = customerSite?.Name,
            LinkedOperationPlanId = linkedPlan?.Id,
            LinkedOperationPlanCode = linkedPlan?.Code,
            SubmittedAt = r.SubmittedAt,
            ApprovedAt = r.ApprovedAt,
            ApprovalDueAt = r.ApprovalDueAt,
            ResolutionDueAt = r.ResolutionDueAt,
            SlaDueAt = slaDueAt,
            SlaStage = OperationSlaService.GetActiveStage(r.Status),
            Lines = lines,
            ApprovalTasks = approvals,
            WorkItems = workItems,
            AiInsights = aiInsights,
            ActivityLog = activityLog,
            Comments = comments,
            ProgressLogs = progressLogs,
            Attachments = attachments,
            Assignments = assignments,
            CanEdit = canEditDraft,
            CanSubmit = nextStates.Contains(OperationStatus.Submitted) && await CanSubmitRequestAsync(r),
            CanCancel = nextStates.Contains(OperationStatus.Cancelled) && await CanCancelRequestAsync(r),
            CanStartWork = nextStates.Contains(OperationStatus.InProgress) && r.Status == OperationStatus.Approved && canManageWork,
            CanComplete = nextStates.Contains(OperationStatus.Completed) && canManageWork,
            CanManageWork = canManageWork,
            CanAddLine = ((r.Status is OperationStatus.Draft or OperationStatus.Rejected) && r.RequestedByUserId == tenant.UserId)
                || (canSupportWork && r.Status is not (OperationStatus.Completed or OperationStatus.Cancelled)),
            CanAddComment = canSupportWork && r.Status != OperationStatus.Cancelled,
            CanAddProgress = r.Status == OperationStatus.InProgress && canSupportWork,
            CanUploadAttachment = canSupportWork && r.Status != OperationStatus.Cancelled,
            CanManageAssignments = canManageAssignments,
            CurrentProgressPercent = currentProgress,
            LastProgressAt = progressLogs.FirstOrDefault()?.CreatedAt,
            IsProgressStale = isProgressStale,
            NextStatuses = nextStates.Select(s => s.ToString()).ToList(),
            AssignableUsers = await db.AppUsers
                .AsNoTracking()
                .Where(u => u.TenantId == tenant.TenantId && u.Status == UserStatus.Active && !u.IsDeleted)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = string.IsNullOrWhiteSpace(u.JobTitle) ? u.FullName : u.FullName + " - " + u.JobTitle })
                .ToListAsync(),
            AssignableDepartments = await GetDepartmentOptionsAsync(tenant.TenantId)
        };
    }

    public async Task<OperationRequestCreateViewModel> GetCreateFormAsync(Guid? templateId = null)
    {
        var tid = tenant.TenantId;
        var vm = new OperationRequestCreateViewModel
        {
            TemplateId = templateId,
            Departments = await GetDepartmentOptionsAsync(tid),
            Customers = await db.Customers.Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted)
                .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Code + " - " + c.Name })
                .ToListAsync(),
            Products = await db.ProductServices.Where(p => p.TenantId == tid && p.IsActive && !p.IsDeleted).OrderBy(p => p.Name)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.Code + " - " + p.Name + (p.StandardPrice.HasValue ? $" ({p.StandardPrice:N0} VND)" : "") })
                .ToListAsync(),
            Templates = await db.OperationRequestTemplates
                .Where(t => t.TenantId == tid && t.IsActive && !t.IsDeleted)
                .OrderBy(t => t.Title)
                .Select(t => new SelectOption { Value = t.Id.ToString(), Text = t.Title })
                .ToListAsync()
        };

        if (templateId.HasValue)
        {
            var template = await db.OperationRequestTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == templateId.Value && t.TenantId == tid && t.IsActive && !t.IsDeleted);
            if (template != null)
            {
                vm.Title = template.Title;
                vm.Type = template.Type;
                vm.Priority = template.Priority;
                vm.OrganizationUnitId = template.DefaultDepartmentId;
                vm.Description = template.Description;
                vm.Lines = await BuildLineInputsFromTemplateAsync(template.DefaultLinesJson);
                if (vm.Lines.Any()) vm.TotalAmount = vm.Lines.Sum(l => l.LineAmount);
            }
        }

        return vm;
    }

    public async Task<OperationRequestEditViewModel?> GetEditFormAsync(Guid id)
    {
        var r = await db.OperationRequests.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted);
        if (r is null || r.Status is not (OperationStatus.Draft or OperationStatus.Rejected)) return null;
        if (r.RequestedByUserId != tenant.UserId && !await CanSupportRequestAsync(r.Id)) return null;

        var tid = tenant.TenantId;
        return new OperationRequestEditViewModel
        {
            Id = r.Id,
            RequestNo = r.RequestNo,
            Title = r.Title,
            Type = r.Type,
            OrganizationUnitId = r.OrganizationUnitId,
            CustomerId = r.CustomerId,
            Priority = r.Priority,
            DueDate = r.DueDate,
            Description = r.Description,
            TotalAmount = r.TotalAmount,
            Departments = await GetDepartmentOptionsAsync(tid),
            Customers = await db.Customers.Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted)
                .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync()
        };
    }

    public async Task<OperationRequestTemplateListViewModel> GetTemplatesAsync(string? search = null)
    {
        var tid = tenant.TenantId;
        var query = db.OperationRequestTemplates
            .AsNoTracking()
            .Include(t => t.DefaultDepartment)
            .Where(t => t.TenantId == tid && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.Contains(search) || t.Type.Contains(search));

        var templates = await query
            .OrderByDescending(t => t.IsActive)
            .ThenByDescending(t => t.UsageCount)
            .ThenBy(t => t.Title)
            .ToListAsync();

        return new OperationRequestTemplateListViewModel
        {
            SearchTerm = search,
            Items = templates.Select(t => new OperationRequestTemplateItem
            {
                Id = t.Id,
                Title = t.Title,
                Type = t.Type,
                Priority = t.Priority.ToString(),
                Department = t.DefaultDepartment?.Name ?? "",
                DefaultLineCount = DeserializeTemplateLines(t.DefaultLinesJson).Count,
                IsActive = t.IsActive,
                UsageCount = t.UsageCount,
                CreatedAt = t.CreatedAt,
                LastUsedAt = t.LastUsedAt
            }).ToList()
        };
    }

    public async Task<OperationRequestTemplateEditViewModel?> GetTemplateFormAsync(Guid? id = null)
    {
        var tid = tenant.TenantId;
        var departments = await GetDepartmentOptionsAsync(tid);

        if (!id.HasValue)
            return new OperationRequestTemplateEditViewModel { Departments = departments, IsActive = true };

        var template = await db.OperationRequestTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id.Value && t.TenantId == tid && !t.IsDeleted);
        if (template is null) return null;

        return new OperationRequestTemplateEditViewModel
        {
            Id = template.Id,
            Title = template.Title,
            Type = template.Type,
            Priority = template.Priority,
            DefaultDepartmentId = template.DefaultDepartmentId,
            Description = template.Description,
            DefaultLinesJson = template.DefaultLinesJson,
            IsActive = template.IsActive,
            Departments = departments
        };
    }

    public async Task<OperationStatisticsViewModel> GetStatisticsAsync()
    {
        var tid = tenant.TenantId;
        var baseQ = db.OperationRequests.Where(r => r.TenantId == tid && !r.IsDeleted);

        var total = await baseQ.CountAsync();
        var completed = await baseQ.CountAsync(r => r.Status == OperationStatus.Completed);
        var cancelled = await baseQ.CountAsync(r => r.Status == OperationStatus.Cancelled);
        var activeTotal = total - cancelled;
        var completionRate = activeTotal > 0 ? (decimal)completed / activeTotal * 100 : 0;

        var completedRequests = await baseQ.Where(r => r.Status == OperationStatus.Completed).ToListAsync();
        double avgProcessingDays = completedRequests.Any()
            ? completedRequests.Average(r => ((r.UpdatedAt ?? DateTimeOffset.UtcNow) - r.CreatedAt).TotalDays)
            : 0;

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var requestsWithDue = await baseQ
            .Where(r => r.Status != OperationStatus.Cancelled
                && (r.ApprovalDueAt.HasValue || r.ResolutionDueAt.HasValue || r.DueDate.HasValue))
            .ToListAsync();
        decimal slaComplianceRate = 100;
        if (requestsWithDue.Any())
        {
            var compliantCount = requestsWithDue.Count(r => IsSlaCompliant(r, now, today));
            slaComplianceRate = (decimal)compliantCount / requestsWithDue.Count * 100;
        }

        var priorityGroup = await baseQ.GroupBy(r => r.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync();
        var priorityList = priorityGroup.Select(g => new PriorityStatItem
        {
            Priority = g.Priority.ToString(),
            PriorityLabel = g.Priority switch
            {
                PriorityLevel.Low => "Thấp",
                PriorityLevel.Normal => "Bình thường",
                PriorityLevel.High => "Cao",
                PriorityLevel.Critical => "Nghiêm trọng",
                _ => g.Priority.ToString()
            },
            Count = g.Count
        }).ToList();
        var deptGroup = await baseQ
            .Join(db.OrganizationUnits, r => r.OrganizationUnitId, o => o.Id, (r, o) => new { r, o })
            .GroupBy(x => x.o.Name)
            .Select(g => new DepartmentStatItem { DepartmentName = g.Key, Count = g.Count() })
            .ToListAsync();
        var weeklyTrend = new List<WeeklyTrendItem>();
        var todayDate = DateTime.Today;
        for (var i = 6; i >= 0; i--)
        {
            var date = todayDate.AddDays(-i);
            weeklyTrend.Add(new WeeklyTrendItem
            {
                DateLabel = date.ToString("dd/MM"),
                CreatedCount = await baseQ.CountAsync(r => r.CreatedAt.Date == date),
                CompletedCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Completed && r.UpdatedAt.HasValue && r.UpdatedAt.Value.Date == date)
            });
        }

        return new OperationStatisticsViewModel
        {
            CompletionRate = Math.Round(completionRate, 1),
            AvgProcessingDays = Math.Round(avgProcessingDays, 1),
            SlaComplianceRate = Math.Round(slaComplianceRate, 1),
            ByPriority = priorityList,
            ByDepartment = deptGroup,
            WeeklyTrend = weeklyTrend
        };
    }

    private static Expression<Func<OperationRequest, bool>> BuildOverdueQuery(DateTimeOffset now, DateOnly today)
    {
        return r => ((r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview) && r.ApprovalDueAt.HasValue && r.ApprovalDueAt < now)
            || ((r.Status == OperationStatus.Approved || r.Status == OperationStatus.InProgress || r.Status == OperationStatus.OnHold) && r.ResolutionDueAt.HasValue && r.ResolutionDueAt < now)
            || (!r.ApprovalDueAt.HasValue && !r.ResolutionDueAt.HasValue && r.DueDate.HasValue && r.DueDate.Value < today && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled);
    }

    private async Task<List<OperationCommentViewModel>> GetThreadedCommentsAsync(Guid requestId)
    {
        var commentRows = await db.Set<OperationComment>()
            .AsNoTracking()
            .Where(c => c.OperationRequestId == requestId && c.TenantId == tenant.TenantId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Join(db.AppUsers.AsNoTracking(), c => c.AuthorUserId, u => u.Id, (c, u) => new OperationCommentViewModel
            {
                Id = c.Id,
                Content = c.Content,
                AuthorUserId = c.AuthorUserId,
                AuthorName = u.FullName,
                Type = c.Type,
                ParentCommentId = c.ParentCommentId,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
        var commentMap = commentRows.ToDictionary(c => c.Id);
        foreach (var comment in commentRows.Where(c => c.ParentCommentId.HasValue))
        {
            if (commentMap.TryGetValue(comment.ParentCommentId!.Value, out var parent))
            {
                parent.Replies.Add(comment);
            }
        }

        return commentRows
            .Where(c => !c.ParentCommentId.HasValue || !commentMap.ContainsKey(c.ParentCommentId.Value))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
    }

    private async Task<List<OperationAssignmentItem>> GetAssignmentsAsync(Guid requestId)
    {
        var assignmentEntities = await db.OperationRequestAssignments
            .AsNoTracking()
            .Include(a => a.AssignedUser)
            .Include(a => a.OrganizationUnit)
            .Where(a => a.OperationRequestId == requestId && a.TenantId == tenant.TenantId && a.IsActive && !a.IsDeleted)
            .OrderBy(a => a.Role)
            .ThenBy(a => a.AssignedAt)
            .ToListAsync();

        return assignmentEntities.Select(a => new OperationAssignmentItem
        {
            Id = a.Id,
            Role = a.Role,
            AssignedUserId = a.AssignedUserId,
            AssignedUserName = a.AssignedUser?.FullName,
            OrganizationUnitId = a.OrganizationUnitId,
            OrganizationUnitName = a.OrganizationUnit?.Name,
            AssignedAt = a.AssignedAt,
            Note = a.Note
        }).ToList();
    }

    private async Task<List<SelectOption>> GetDepartmentOptionsAsync(Guid tenantId) =>
        await db.OrganizationUnits
            .Where(o => o.TenantId == tenantId && o.IsActive && !o.IsDeleted)
            .OrderBy(o => o.Name)
            .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();

    private async Task<List<OrderLineInputViewModel>> BuildLineInputsFromTemplateAsync(string? defaultLinesJson)
    {
        var definitions = DeserializeTemplateLines(defaultLinesJson);
        var productIds = definitions.Where(l => l.ProductServiceId.HasValue).Select(l => l.ProductServiceId!.Value).Distinct().ToList();
        var productNames = productIds.Any()
            ? await db.ProductServices
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name)
            : new Dictionary<Guid, string>();

        return definitions.Select(l => new OrderLineInputViewModel
        {
            ProductServiceId = l.ProductServiceId,
            ProductName = l.ProductServiceId.HasValue && productNames.TryGetValue(l.ProductServiceId.Value, out var productName)
                ? productName
                : l.ProductName,
            Quantity = l.Quantity <= 0 ? 1 : l.Quantity,
            UnitPrice = l.UnitPrice,
            Note = l.Note
        }).ToList();
    }

    private static List<OperationRequestTemplateLineDefinition> DeserializeTemplateLines(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<OperationRequestTemplateLineDefinition>>(json, TemplateJsonOptions)?
                .Where(l => l.Quantity > 0)
                .ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private bool IsOperationAdmin() =>
        tenant.HasRole(OperationRoles.SystemAdmin) || tenant.HasRole(OperationRoles.TenantAdmin);

    private bool HasLegacyOperationManagerRole() =>
        IsOperationAdmin() || tenant.HasRole(OperationRoles.DepartmentManager);

    private bool HasLegacyOperationContributorRole() =>
        HasLegacyOperationManagerRole() || tenant.HasRole(OperationRoles.Staff);

    private bool CanManageOperationAssignments() =>
        IsOperationAdmin() || tenant.HasRole(OperationRoles.DepartmentManager);

    private async Task<List<Guid>> GetCurrentUserDepartmentIdsAsync()
    {
        var tid = tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var departmentIds = await db.EmployeeDepartmentAssignments
            .Where(a => a.TenantId == tid
                && !a.IsDeleted
                && a.EmployeeProfile != null
                && a.EmployeeProfile.UserId == tenant.UserId
                && a.EffectiveFrom <= today
                && (!a.EffectiveTo.HasValue || a.EffectiveTo.Value >= today))
            .Select(a => a.OrganizationUnitId)
            .ToListAsync();

        var primaryDepartmentId = await db.AppUsers
            .AsNoTracking()
            .Where(u => u.Id == tenant.UserId && u.TenantId == tid && !u.IsDeleted)
            .Select(u => u.OrganizationUnitId)
            .FirstOrDefaultAsync();

        if (primaryDepartmentId.HasValue) departmentIds.Add(primaryDepartmentId.Value);
        return departmentIds.Distinct().ToList();
    }

    private async Task<OperationAssignmentAccess> GetAssignmentAccessAsync(Guid requestId)
    {
        var activeAssignments = await db.OperationRequestAssignments
            .AsNoTracking()
            .Where(a => a.TenantId == tenant.TenantId
                && a.OperationRequestId == requestId
                && a.IsActive
                && !a.IsDeleted)
            .Select(a => new { a.Role, a.AssignedUserId, a.OrganizationUnitId })
            .ToListAsync();
        if (!activeAssignments.Any()) return new(false, false, false, false);

        var departmentIds = await GetCurrentUserDepartmentIdsAsync();
        var matchedRoles = activeAssignments
            .Where(a => a.AssignedUserId == tenant.UserId
                || (a.OrganizationUnitId.HasValue && departmentIds.Contains(a.OrganizationUnitId.Value)))
            .Select(a => a.Role)
            .Distinct()
            .ToList();

        return new(
            true,
            matchedRoles.Contains(OperationAssignmentRole.Primary),
            matchedRoles.Contains(OperationAssignmentRole.Support),
            matchedRoles.Contains(OperationAssignmentRole.Watcher));
    }

    private async Task<bool> CanSupportRequestAsync(Guid requestId)
    {
        if (IsOperationAdmin()) return true;
        var access = await GetAssignmentAccessAsync(requestId);
        return access.HasAssignments
            ? access.HasPrimary || access.HasSupport
            : HasLegacyOperationContributorRole();
    }

    private async Task<bool> CanSubmitRequestAsync(OperationRequest request)
    {
        if (IsOperationAdmin() || request.RequestedByUserId == tenant.UserId) return true;
        var access = await GetAssignmentAccessAsync(request.Id);
        return access.HasAssignments
            ? access.HasPrimary || access.HasSupport
            : HasLegacyOperationContributorRole();
    }

    private async Task<bool> CanCancelRequestAsync(OperationRequest request)
    {
        if (IsOperationAdmin() || request.RequestedByUserId == tenant.UserId) return true;
        var access = await GetAssignmentAccessAsync(request.Id);
        return access.HasAssignments
            ? access.HasPrimary
            : HasLegacyOperationManagerRole();
    }

    private static bool IsSlaCompliant(OperationRequest request, DateTimeOffset now, DateOnly today)
    {
        var slaDueAt = request.Status == OperationStatus.Completed
            ? request.ResolutionDueAt ?? request.ApprovalDueAt
            : OperationSlaService.GetActiveDueAt(request.Status, request.ApprovalDueAt, request.ResolutionDueAt);
        if (slaDueAt.HasValue)
        {
            var checkpoint = request.Status == OperationStatus.Completed
                ? request.UpdatedAt ?? now
                : now;
            return checkpoint <= slaDueAt.Value;
        }
        if (!request.DueDate.HasValue) return true;
        if (request.Status == OperationStatus.Completed)
        {
            var completedDate = request.UpdatedAt.HasValue ? DateOnly.FromDateTime(request.UpdatedAt.Value.Date) : DateOnly.FromDateTime(DateTime.Today);
            return completedDate <= request.DueDate.Value;
        }

        return today <= request.DueDate.Value;
    }
}
