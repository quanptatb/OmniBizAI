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

    public OperationPlanService(ApplicationDbContext db, ITenantContext tenant, GeminiService gemini)
    {
        _db = db;
        _tenant = tenant;
        _gemini = gemini;
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
        var seq = await _db.OperationPlans.CountAsync(p => p.TenantId == tid) + 1;
        var code = $"OPP-{seq:D4}";
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

    public async Task<bool> CreateTaskAsync(PlanTaskCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans.FindAsync(vm.PlanId);
        if (plan == null || plan.TenantId != tid) return false;

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
        
        if (plan.Status == "Draft") plan.Status = "Approved"; // Auto approve when task added

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string> AnalyzePlanWithAiAsync(Guid planId)
    {
        var plan = await GetPlanDetailAsync(planId);
        if (plan == null) return "Plan not found.";

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
}
