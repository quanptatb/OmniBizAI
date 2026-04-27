using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniBizAI.Models.Common;
using OmniBizAI.Services.Finance;
using OmniBizAI.ViewModels.Finance;

namespace OmniBizAI.Controllers;

/// <summary>
/// Controller quản lý ngân sách.
/// Blueprint mục 3.4 SRP: Controller mỏng — chỉ nhận request, validate cơ bản, gọi Service và trả kết quả.
/// Blueprint mục 8 Route: /Budgets, /Budgets/Details/{id}, /Budgets/UtilizationJson/{id}
/// 
/// Quy tắc bắt buộc:
/// - KHÔNG chứa logic tính toán hay truy vấn EF Core trực tiếp
/// - JSON endpoint phụ trợ bọc trong ApiResponse&lt;T&gt;
/// - Trả View() cho page action, JSON cho endpoint phụ trợ
/// </summary>
[Authorize]
public class BudgetsController : Controller
{
    private readonly IBudgetService _budgetService;
    private readonly ILogger<BudgetsController> _logger;

    public BudgetsController(IBudgetService budgetService, ILogger<BudgetsController> logger)
    {
        _budgetService = budgetService;
        _logger = logger;
    }

    /// <summary>
    /// GET /Budgets — Danh sách ngân sách có phân trang, filter, search.
    /// Blueprint mục 8: MVC GET page action trả View.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] BudgetFilter filter,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _budgetService.GetBudgetsAsync(filter, userId, cancellationToken);

        // Truyền filter xuống View để giữ lại giá trị bộ lọc khi phân trang
        ViewBag.Filter = filter;

        return View(result);
    }

    /// <summary>
    /// GET /Budgets/Details/{id} — Chi tiết ngân sách.
    /// Blueprint mục 3.8: trả NotFound nếu không tìm thấy hoặc ngoài scope.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        try
        {
            var utilization = await _budgetService.GetUtilizationAsync(id, userId, cancellationToken);

            // Lấy thêm thông tin đầy đủ từ danh sách (filter theo ID)
            var budgetFilter = new BudgetFilter { Page = 1, PageSize = 1 };
            var budgetResult = await _budgetService.GetBudgetsAsync(budgetFilter, userId, cancellationToken);
            var budget = budgetResult.Items.FirstOrDefault(b => b.Id == id);

            if (budget is null)
            {
                return NotFound();
            }

            // Truyền cả budget detail và utilization info cho View
            ViewBag.Utilization = utilization;
            return View(budget);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// GET /Budgets/UtilizationJson/{id} — JSON endpoint phụ trợ cho chart/widget.
    /// Blueprint mục 7.3 và 8.3: JSON phụ trợ có hậu tố Json, bọc trong ApiResponse&lt;T&gt;.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> UtilizationJson(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        try
        {
            var utilization = await _budgetService.GetUtilizationAsync(id, userId, cancellationToken);

            // Blueprint mục 3.7: JSON endpoint phụ trợ bọc trong ApiResponse<T>
            var response = ApiResponse<BudgetUtilizationDto>.Ok(
                utilization,
                "Lấy thông tin utilization thành công");

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            var response = ApiResponse<BudgetUtilizationDto>.Fail(
                ex.Message,
                HttpContext.TraceIdentifier);

            return NotFound(response);
        }
    }

    // ============================================================
    // HELPER: Lấy UserId của user đang đăng nhập
    // ============================================================

    /// <summary>
    /// Lấy Guid UserId từ Identity claims.
    /// Trong giai đoạn BAO-02 (chưa có Auth module đầy đủ), 
    /// trả về Guid.Empty nếu chưa đăng nhập.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Không thể parse UserId từ claims. Dùng Guid.Empty cho stub.");
            return Guid.Empty;
        }

        return userId;
    }
}
