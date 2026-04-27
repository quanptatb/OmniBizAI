using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Common;
using OmniBizAI.Models.Entities.Finance;
using OmniBizAI.Services.System;
using OmniBizAI.ViewModels.Finance;

namespace OmniBizAI.Services.Finance;

/// <summary>
/// Implementation của IBudgetService.
/// 
/// Tuân thủ nghiêm ngặt các quy tắc kiến trúc Blueprint:
/// 1. Data Scope: mọi query đều qua IDataScopeService.ApplyScope()
/// 2. AsNoTracking: tất cả read-only query dùng .AsNoTracking()
/// 3. Controller mỏng: toàn bộ logic tính toán nằm trong service
/// 4. Không query trực tiếp _context.Budgets bỏ qua scope
/// </summary>
public sealed class BudgetService : IBudgetService
{
    private readonly ApplicationDbContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly ILogger<BudgetService> _logger;

    public BudgetService(
        ApplicationDbContext context,
        IDataScopeService dataScopeService,
        ILogger<BudgetService> logger)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PagedResult<BudgetDto>> GetBudgetsAsync(
        BudgetFilter filter, Guid userId, CancellationToken cancellationToken = default)
    {
        // ========================================
        // BƯỚC 1: Resolve Data Scope cho user hiện tại
        // Blueprint mục 6.2: mọi query phải đi qua ApplyScope trước khi materialize.
        // ========================================
        var scope = await _dataScopeService.GetScopeAsync(userId, cancellationToken);

        // Lấy queryable gốc với AsNoTracking (read-only performance)
        // và áp dụng soft-delete filter
        var query = _context.Budgets
            .AsNoTracking()
            .Where(b => !b.IsDeleted);

        // ========================================
        // BƯỚC 2: Áp dụng Data Scope vào query
        // KHÔNG ĐƯỢC bỏ qua bước này — đây là quy tắc bắt buộc của kiến trúc.
        // ========================================
        query = _dataScopeService.ApplyScope(query, scope);

        // ========================================
        // BƯỚC 3: Áp dụng các bộ lọc từ BudgetFilter
        // ========================================

        // Lọc theo trạng thái ngân sách
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(b => b.Status == filter.Status);
        }

        // Lọc theo phòng ban
        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(b => b.DepartmentId == filter.DepartmentId.Value);
        }

        // Lọc theo danh mục ngân sách
        if (filter.CategoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == filter.CategoryId.Value);
        }

        // Lọc theo khoảng thời gian (dựa trên kỳ tài chính)
        if (filter.DateFrom.HasValue)
        {
            query = query.Where(b => b.FiscalPeriod.StartDate >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            query = query.Where(b => b.FiscalPeriod.EndDate <= filter.DateTo.Value);
        }

        // Tìm kiếm theo tên hoặc mã ngân sách
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = filter.Search.Trim().ToLower();
            query = query.Where(b => b.Name.ToLower().Contains(searchTerm));
        }

        // ========================================
        // BƯỚC 4: Đếm tổng số bản ghi (trước phân trang)
        // ========================================
        var totalCount = await query.CountAsync(cancellationToken);

        // ========================================
        // BƯỚC 5: Sắp xếp
        // ========================================
        query = ApplySorting(query, filter.SortBy, filter.SortOrder);

        // ========================================
        // BƯỚC 6: Phân trang và project sang DTO
        // Include navigation properties để lấy tên phòng ban, danh mục, kỳ tài chính.
        // ========================================
        var items = await query
            .Include(b => b.Department)
            .Include(b => b.Category)
            .Include(b => b.FiscalPeriod)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => MapToDto(b))
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "GetBudgetsAsync: userId={UserId}, scope={ScopeRole}, totalCount={TotalCount}, page={Page}",
            userId, scope.Role, totalCount, filter.Page);

        return new PagedResult<BudgetDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<BudgetUtilizationDto> GetUtilizationAsync(
        Guid budgetId, Guid userId, CancellationToken cancellationToken = default)
    {
        // ========================================
        // BƯỚC 1: Resolve Data Scope
        // ========================================
        var scope = await _dataScopeService.GetScopeAsync(userId, cancellationToken);

        // ========================================
        // BƯỚC 2: Query budget với data scope (KHÔNG bỏ qua scope)
        // ========================================
        var query = _context.Budgets
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.Id == budgetId);

        // Áp dụng Data Scope — bắt buộc theo Blueprint
        query = _dataScopeService.ApplyScope(query, scope);

        var budget = await query.FirstOrDefaultAsync(cancellationToken);

        if (budget is null)
        {
            _logger.LogWarning(
                "GetUtilizationAsync: Budget {BudgetId} không tìm thấy hoặc ngoài scope user {UserId}",
                budgetId, userId);

            // Blueprint mục 3.8: Throw NotFoundException nếu không thấy hoặc ngoài scope
            throw new KeyNotFoundException(
                $"Không tìm thấy ngân sách với ID '{budgetId}' hoặc bạn không có quyền truy cập.");
        }

        // ========================================
        // BƯỚC 3: Tính toán Utilization
        // Công thức theo Blueprint mục 7.3 Rules - Budget:
        // - RemainingAmount = AllocatedAmount - SpentAmount - CommittedAmount
        // - UtilizationPct = (SpentAmount + CommittedAmount) / AllocatedAmount * 100
        // ========================================
        var utilizationDto = CalculateUtilization(budget);

        _logger.LogDebug(
            "GetUtilizationAsync: budgetId={BudgetId}, utilization={UtilizationPct}%, warning={WarningLevel}",
            budgetId, utilizationDto.UtilizationPct, utilizationDto.WarningLevel);

        return utilizationDto;
    }

    // ============================================================
    // PRIVATE HELPER METHODS
    // ============================================================

    /// <summary>
    /// Tính toán utilization và warning level cho một budget.
    /// 
    /// Công thức nghiệp vụ (Blueprint mục 7.3):
    /// - RemainingAmount = AllocatedAmount - SpentAmount - CommittedAmount
    /// - UtilizationPct = (SpentAmount + CommittedAmount) / AllocatedAmount * 100
    /// 
    /// Logic Warning Level:
    /// - UtilizationPct > 100 → "Critical" (đỏ) — vượt ngân sách
    /// - UtilizationPct >= WarningThreshold (80) → "Warning" (vàng) — gần vượt
    /// - Còn lại → "Normal" (xanh)
    /// </summary>
    private static BudgetUtilizationDto CalculateUtilization(Budget budget)
    {
        // Tính remaining amount
        var remainingAmount = budget.AllocatedAmount - budget.SpentAmount - budget.CommittedAmount;

        // Tính utilization percentage (tránh chia cho 0)
        var utilizationPct = budget.AllocatedAmount > 0
            ? (budget.SpentAmount + budget.CommittedAmount) / budget.AllocatedAmount * 100m
            : 0m;

        // Làm tròn 2 chữ số thập phân
        utilizationPct = Math.Round(utilizationPct, 2);

        // Xác định mức cảnh báo theo quy tắc Blueprint
        var warningLevel = DetermineWarningLevel(utilizationPct, budget.WarningThreshold);

        return new BudgetUtilizationDto
        {
            BudgetId = budget.Id,
            BudgetName = budget.Name,
            AllocatedAmount = budget.AllocatedAmount,
            SpentAmount = budget.SpentAmount,
            CommittedAmount = budget.CommittedAmount,
            RemainingAmount = remainingAmount,
            UtilizationPct = utilizationPct,
            WarningLevel = warningLevel
        };
    }

    /// <summary>
    /// Xác định mức cảnh báo dựa trên utilization và threshold.
    /// Blueprint mục 7.3:
    /// - > 100% → Critical (đỏ)
    /// - >= threshold (mặc định 80%) → Warning (vàng)
    /// - Còn lại → Normal (xanh)
    /// </summary>
    private static string DetermineWarningLevel(decimal utilizationPct, decimal warningThreshold)
    {
        if (utilizationPct > 100m)
            return nameof(WarningLevel.Critical);

        if (utilizationPct >= warningThreshold)
            return nameof(WarningLevel.Warning);

        return nameof(WarningLevel.Normal);
    }

    /// <summary>
    /// Áp dụng sắp xếp cho danh sách budget.
    /// Mặc định sắp xếp theo ngày tạo giảm dần.
    /// </summary>
    private static IQueryable<Budget> ApplySorting(
        IQueryable<Budget> query, string? sortBy, string sortOrder)
    {
        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToLower() switch
        {
            "name" => isDescending
                ? query.OrderByDescending(b => b.Name)
                : query.OrderBy(b => b.Name),

            "allocatedamount" => isDescending
                ? query.OrderByDescending(b => b.AllocatedAmount)
                : query.OrderBy(b => b.AllocatedAmount),

            "utilizationpct" => isDescending
                ? query.OrderByDescending(b => b.UtilizationPct)
                : query.OrderBy(b => b.UtilizationPct),

            "status" => isDescending
                ? query.OrderByDescending(b => b.Status)
                : query.OrderBy(b => b.Status),

            "department" => isDescending
                ? query.OrderByDescending(b => b.Department.Name)
                : query.OrderBy(b => b.Department.Name),

            // Mặc định: sắp xếp theo ngày tạo (mới nhất trước)
            _ => isDescending
                ? query.OrderByDescending(b => b.CreatedAt)
                : query.OrderBy(b => b.CreatedAt)
        };
    }

    /// <summary>
    /// Map entity Budget sang BudgetDto.
    /// Sử dụng expression-based mapping cho EF Core projection.
    /// </summary>
    private static BudgetDto MapToDto(Budget b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        DepartmentId = b.DepartmentId,
        DepartmentName = b.Department != null ? b.Department.Name : string.Empty,
        CategoryId = b.CategoryId,
        CategoryName = b.Category != null ? b.Category.Name : string.Empty,
        AllocatedAmount = b.AllocatedAmount,
        SpentAmount = b.SpentAmount,
        CommittedAmount = b.CommittedAmount,
        RemainingAmount = b.RemainingAmount,
        UtilizationPct = b.UtilizationPct,
        Status = b.Status,
        FiscalPeriodName = b.FiscalPeriod != null ? b.FiscalPeriod.Name : string.Empty,
        WarningThreshold = b.WarningThreshold
    };
}
