using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class OperationPlanService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly GeminiService _gemini;
    private readonly NotificationService _notif;
    private readonly NumberSequenceService _seq;

    public OperationPlanService(ApplicationDbContext db, ITenantContext tenant, GeminiService gemini, NotificationService notif, NumberSequenceService seq)
    {
        _db = db;
        _tenant = tenant;
        _gemini = gemini;
        _notif = notif;
        _seq = seq;
    }

    public async Task<List<OperationPlanListViewModel>> GetPlansAsync()
    {
        var tid = _tenant.TenantId;
        var plans = await _db.OperationPlans
            .Include(p => p.Tasks)
            .Where(p => p.TenantId == tid && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return plans.Select(p => {
            int progress = 0;
            if (p.Tasks.Any()) {
                progress = (int)p.Tasks.Average(t => t.ProgressPercent);
            }
            return new OperationPlanListViewModel
            {
                Id = p.Id,
                Code = p.Code,
                Title = p.Title,
                PlanType = p.PlanType,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status,
                TaskCount = p.Tasks.Count,
                ProgressPercent = progress
            };
        }).ToList();
    }

    public async Task<OperationPlanDetailViewModel?> GetPlanDetailAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.AssignedUser)
            .Include(p => p.Tasks)
                .ThenInclude(t => t.Equipment)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tid && !p.IsDeleted);

        if (plan == null) return null;

        // Auto check delayed tasks
        bool needSave = false;
        var now = DateTime.UtcNow;
        foreach (var task in plan.Tasks)
        {
            if (task.Status != "Done" && task.EndTime < now && task.Status != "Delayed")
            {
                task.Status = "Delayed";
                needSave = true;
            }
        }
        if (needSave) await _db.SaveChangesAsync();

        int progress = plan.Tasks.Any() ? (int)plan.Tasks.Average(t => t.ProgressPercent) : 0;

        return new OperationPlanDetailViewModel
        {
            Id = plan.Id,
            Code = plan.Code,
            Title = plan.Title,
            PlanType = plan.PlanType,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            Status = plan.Status,
            Notes = plan.Notes,
            ProgressPercent = progress,
            Tasks = plan.Tasks.OrderBy(t => t.StartTime).Select(t => new PlanTaskViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                AssignedUserName = t.AssignedUser?.FullName,
                EquipmentName = t.Equipment?.Name,
                Status = t.Status,
                ProgressPercent = t.ProgressPercent
            }).ToList()
        };
    }

    public async Task<Guid> CreatePlanAsync(OperationPlanCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var code = await _seq.NextCodeAsync("OperationPlan", "OPP-");
        var plan = new OperationPlan
        {
            TenantId = tid,
            Code = code,
            Title = vm.Title,
            PlanType = vm.PlanType,
            StartDate = vm.StartDate,
            EndDate = vm.EndDate,
            Notes = vm.Notes,
            Status = "Draft",
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.OperationPlans.Add(plan);
        await _db.SaveChangesAsync();

        await _notif.SendToManagersAsync(
            $"📅 Kế hoạch vận hành mới: {plan.Code}",
            $"{_tenant.UserFullName} đã tạo kế hoạch \"{plan.Title}\" ({plan.PlanType}, {plan.StartDate:dd/MM} → {plan.EndDate:dd/MM}).",
            "OperationPlan", plan.Id);

        return plan.Id;
    }

    public async Task<PlanTaskCreateViewModel> GetCreateTaskFormAsync(Guid planId)
    {
        var tid = _tenant.TenantId;
        var users = await _db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted)
            .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
            .ToListAsync();
            
        var equipments = await _db.Equipments.Where(e => e.TenantId == tid && !e.IsDeleted)
            .Select(e => new SelectOption { Value = e.Id.ToString(), Text = e.Name })
            .ToListAsync();

        return new PlanTaskCreateViewModel
        {
            PlanId = planId,
            StartTime = DateTime.Today,
            EndTime = DateTime.Today.AddDays(1),
            Users = users,
            Equipments = equipments
        };
    }

    public async Task<(bool Success, string Message)> CreateTaskAsync(PlanTaskCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans
            .FirstOrDefaultAsync(p => p.Id == vm.PlanId && p.TenantId == tid && !p.IsDeleted);
        if (plan == null)
            return (false, "Kế hoạch vận hành không tồn tại hoặc bạn không có quyền truy cập.");

        if (vm.EndTime <= vm.StartTime)
        {
            return (false, "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
        }

        if (vm.AssignedUserId.HasValue)
        {
            var workerConflict = await _db.PlanTasks
                .AnyAsync(t => t.TenantId == tid && !t.IsDeleted
                               && t.AssignedUserId == vm.AssignedUserId.Value
                               && t.StartTime < vm.EndTime && t.EndTime > vm.StartTime);
            if (workerConflict)
            {
                var workerName = await _db.AppUsers
                    .Where(u => u.Id == vm.AssignedUserId.Value && u.TenantId == tid)
                    .Select(u => u.FullName).FirstOrDefaultAsync() ?? "nhân viên";
                return (false, $"Xung đột lịch trình: {workerName} đã được phân công công việc khác trong khoảng thời gian này.");
            }
        }

        if (vm.EquipmentId.HasValue)
        {
            var equipmentConflict = await _db.PlanTasks
                .AnyAsync(t => t.TenantId == tid && !t.IsDeleted
                               && t.EquipmentId == vm.EquipmentId.Value
                               && t.StartTime < vm.EndTime && t.EndTime > vm.StartTime);
            if (equipmentConflict)
            {
                var equipmentName = await _db.Equipments
                    .Where(e => e.Id == vm.EquipmentId.Value && e.TenantId == tid)
                    .Select(e => e.Name).FirstOrDefaultAsync() ?? "thiết bị";
                return (false, $"Xung đột tài nguyên: {equipmentName} đang được sử dụng cho công việc khác trong khoảng thời gian này.");
            }
        }

        var task = new PlanTask
        {
            TenantId = tid,
            PlanId = vm.PlanId,
            Name = vm.Name,
            Description = vm.Description,
            StartTime = vm.StartTime,
            EndTime = vm.EndTime,
            AssignedUserId = vm.AssignedUserId,
            EquipmentId = vm.EquipmentId,
            Status = "Todo",
            ProgressPercent = 0,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.PlanTasks.Add(task);

        if (plan.Status == "Draft") plan.Status = "Approved";

        await _db.SaveChangesAsync();

        if (vm.AssignedUserId.HasValue && vm.AssignedUserId.Value != _tenant.UserId)
        {
            await _notif.SendAsync(
                $"🧑‍🔧 Bạn được phân công công việc mới",
                $"{_tenant.UserFullName} đã phân công cho bạn: \"{vm.Name}\" ({vm.StartTime:dd/MM HH:mm} - {vm.EndTime:dd/MM HH:mm}) thuộc kế hoạch {plan.Code}.",
                "PlanTask", task.Id, vm.AssignedUserId.Value);
        }
        else
        {
            await _notif.SendToManagersAsync(
                $"➕ Task mới trong kế hoạch {plan.Code}",
                $"{_tenant.UserFullName} đã thêm task \"{vm.Name}\" vào kế hoạch \"{plan.Title}\".",
                "PlanTask", task.Id);
        }

        return (true, "Đã phân công công việc mới.");
    }

    public async Task<string> AnalyzePlanWithAiAsync(Guid planId)
    {
        var plan = await GetPlanDetailAsync(planId);
        if (plan == null) return "Plan not found.";

        var delayedTasks = plan.Tasks.Where(t => t.Status == "Delayed").ToList();
        if (delayedTasks.Any())
        {
            var names = string.Join(", ", delayedTasks.Take(5).Select(t => t.Name));
            await _notif.SendToManagersAsync(
                $"⚠️ Kế hoạch {plan.Code} có {delayedTasks.Count} task trễ hạn",
                $"Các task bị trễ: {names}{(delayedTasks.Count > 5 ? "..." : "")}",
                "OperationPlan", planId);
        }

        var prompt = $"Phân tích Kế hoạch vận hành/sản xuất '{plan.Title}' (Từ {plan.StartDate:d} đến {plan.EndDate:d}).\n" +
                     $"Tiến độ hiện tại: {plan.ProgressPercent}%.\n" +
                     $"Các công việc:\n";

        foreach(var t in plan.Tasks) {
            prompt += $"- {t.Name}: Hạn {t.EndTime:g}, Trạng thái: {t.Status}, Phụ trách: {t.AssignedUserName ?? "Trống"}, Thiết bị: {t.EquipmentName ?? "Trống"}\n";
        }

        prompt += "\nHãy chỉ ra rủi ro (đặc biệt là các công việc bị 'Delayed') và đưa ra đề xuất điều chỉnh lịch trình ngắn gọn.";

        var response = await _gemini.GenerateAsync(
            "Bạn là trợ lý AI chuyên nghiệp phân tích Kế hoạch Vận hành và Quản lý Rủi ro.",
            prompt);

        return response.Success ? response.Text : response.ErrorMessage ?? "Lỗi khi gọi AI.";
    }

    public async Task<(bool Success, string Message, Guid PlanId)> UpdateTaskStatusAsync(Guid taskId, string newStatus, int? progressPercent)
    {
        var tid = _tenant.TenantId;
        var task = await _db.PlanTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.TenantId == tid && !t.IsDeleted);
        if (task == null) return (false, "Không tìm thấy công việc.", Guid.Empty);

        var allowed = new[] { "Todo", "InProgress", "Done", "Cancelled" };
        if (!allowed.Contains(newStatus))
            return (false, "Trạng thái không hợp lệ.", task.PlanId);

        var isAssignee = task.AssignedUserId == _tenant.UserId;
        var managerRoles = new[] { "DEPARTMENT_MANAGER", "EXECUTIVE", "TENANT_ADMIN", "SYSTEM_ADMIN" };
        var isManager = managerRoles.Any(r => _tenant.HasRole(r));
        if (!isAssignee && !isManager)
            return (false, "Chỉ người được giao hoặc quản lý mới có thể cập nhật.", task.PlanId);

        task.Status = newStatus;
        if (progressPercent.HasValue)
            task.ProgressPercent = Math.Clamp(progressPercent.Value, 0, 100);
        if (newStatus == "Done") task.ProgressPercent = 100;
        else if (newStatus == "Todo") task.ProgressPercent = 0;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        task.UpdatedByUserId = _tenant.UserId;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "Công việc vừa được người khác cập nhật. Vui lòng tải lại trang để xem thay đổi mới nhất.", task.PlanId);
        }
        return (true, $"Đã cập nhật \"{task.Name}\" → {newStatus} ({task.ProgressPercent}%).", task.PlanId);
    }
}
