using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Services;

public class OperationApprovalService(ApplicationDbContext db, ITenantContext tenant)
{
    public void CreateDepartmentReviewTask(Guid requestId, DateTimeOffset createdAt)
    {
        db.ApprovalTasks.Add(new ApprovalTask
        {
            TenantId = tenant.TenantId,
            TargetType = "OperationRequest",
            TargetId = requestId,
            StepCode = "DEPARTMENT_REVIEW",
            AssignedRole = OperationRoles.DepartmentManager,
            Status = ApprovalStatus.Pending,
            CreatedAt = createdAt,
            CreatedByUserId = tenant.UserId
        });
    }
}
