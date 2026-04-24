using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;

namespace OmniBizAI.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public WorkflowController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet("approval-queue")]
    [Authorize(Roles = "Admin,Director,Manager")]
    public async Task<ActionResult<ApiResponse<PagedResult<ApprovalQueueItemDto>>>> ApprovalQueue([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResult<ApprovalQueueItemDto>>.Ok(await _workflowService.GetApprovalQueueAsync(request, cancellationToken)));
    }

    [HttpPost("workflow-instances/{id:guid}/approve")]
    [Authorize(Roles = "Admin,Director,Manager")]
    public async Task<ActionResult<ApiResponse<object>>> Approve(Guid id, ApprovalActionRequest request, CancellationToken cancellationToken)
    {
        await _workflowService.ApproveAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Workflow approved"));
    }

    [HttpPost("workflow-instances/{id:guid}/reject")]
    [Authorize(Roles = "Admin,Director,Manager")]
    public async Task<ActionResult<ApiResponse<object>>> Reject(Guid id, ApprovalActionRequest request, CancellationToken cancellationToken)
    {
        await _workflowService.RejectAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Workflow rejected"));
    }
}
