using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public sealed class PaymentRequestService(ApplicationDbContext context) : IPaymentRequestService
{
    private static readonly HashSet<string> AllowedPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low",
        "Normal",
        "High",
        "Urgent"
    };

    public async Task<IReadOnlyList<PaymentRequestListItemDto>> GetListAsync(PaymentRequestFilter filter, string userId, CancellationToken cancellationToken = default)
    {
        var query = context.PaymentRequests
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Requester)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<PaymentRequestStatus>(filter.Status, true, out var status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.RequesterId.HasValue)
        {
            query = query.Where(x => x.RequesterId == filter.RequesterId.Value);
        }

        if (filter.VendorId.HasValue)
        {
            query = query.Where(x => x.VendorId == filter.VendorId.Value);
        }

        if (filter.DateFrom.HasValue)
        {
            var from = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.CreatedAt >= from);
        }

        if (filter.DateTo.HasValue)
        {
            var to = filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.CreatedAt <= to);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new PaymentRequestListItemDto
            {
                Id = x.Id,
                RequestNumber = x.RequestNumber,
                Title = x.Title,
                RequesterName = x.Requester != null ? x.Requester.FullName : string.Empty,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                TotalAmount = x.TotalAmount,
                Priority = x.Priority,
                Status = x.Status.ToString(),
                RiskLevel = x.AiRiskLevel ?? "Low",
                CreatedAt = x.CreatedAt,
                SubmittedAt = x.SubmittedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentRequestDetailDto> GetDetailAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var paymentRequest = await LoadDetailQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (paymentRequest is null)
        {
            throw new KeyNotFoundException("Không tìm thấy đề nghị thanh toán.");
        }

        return ToDetailDto(paymentRequest);
    }

    public async Task<PaymentRequestDetailDto> CreateDraftAsync(CreatePaymentRequestRequest request, string userId, CancellationToken cancellationToken = default)
    {
        var normalizedItems = NormalizeAndValidate(request.Items);
        ValidateHeader(request.Title, request.Priority, request.PaymentDueDate);

        var requester = await GetOrCreateRequesterAsync(userId, cancellationToken);
        await ValidateReferencesAsync(request.DepartmentId, request.CategoryId, request.VendorId, request.BudgetId, cancellationToken);

        var now = DateTime.UtcNow;
        var entity = new PaymentRequest
        {
            Id = Guid.NewGuid(),
            CompanyId = requester.CompanyId,
            RequestNumber = await GenerateRequestNumberAsync(now.Year, cancellationToken),
            Title = request.Title.Trim(),
            Description = NormalizeNullable(request.Description),
            DepartmentId = request.DepartmentId,
            RequesterId = requester.Id,
            VendorId = request.VendorId,
            BudgetId = request.BudgetId,
            CategoryId = request.CategoryId,
            Currency = "VND",
            Priority = NormalizePriority(request.Priority),
            Status = PaymentRequestStatus.Draft,
            CreatedAt = now
        };

        ApplyItems(entity, normalizedItems);
        context.PaymentRequests.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(entity.Id, userId, cancellationToken);
    }

    public async Task<PaymentRequestDetailDto> UpdateDraftAsync(Guid id, UpdatePaymentRequestRequest request, string userId, CancellationToken cancellationToken = default)
    {
        var normalizedItems = NormalizeAndValidate(request.Items);
        ValidateHeader(request.Title, request.Priority, request.PaymentDueDate);

        var entity = await context.PaymentRequests
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy đề nghị thanh toán.");
        }

        if (entity.Status != PaymentRequestStatus.Draft)
        {
            throw new InvalidOperationException("Chỉ đề nghị ở trạng thái Draft mới được chỉnh sửa.");
        }

        await ValidateReferencesAsync(request.DepartmentId, request.CategoryId, request.VendorId, request.BudgetId, cancellationToken);

        entity.Title = request.Title.Trim();
        entity.Description = NormalizeNullable(request.Description);
        entity.DepartmentId = request.DepartmentId;
        entity.CategoryId = request.CategoryId;
        entity.VendorId = request.VendorId;
        entity.BudgetId = request.BudgetId;
        entity.Priority = NormalizePriority(request.Priority);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.Items.Clear();
        ApplyItems(entity, normalizedItems);

        await context.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(entity.Id, userId, cancellationToken);
    }

    public async Task<PaymentRequestLookupsDto> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        return new PaymentRequestLookupsDto
        {
            Departments = await context.Departments.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItemDto { Id = x.Id, Name = x.Name }).ToListAsync(cancellationToken),
            Categories = await context.BudgetCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new LookupItemDto { Id = x.Id, Name = x.Name }).ToListAsync(cancellationToken),
            Vendors = await context.Vendors.AsNoTracking().Where(x => x.Status == "Active").OrderBy(x => x.Name).Select(x => new LookupItemDto { Id = x.Id, Name = x.Name }).ToListAsync(cancellationToken),
            Budgets = await context.Budgets.AsNoTracking().Where(x => x.Status == "Active").OrderBy(x => x.Name).Select(x => new LookupItemDto { Id = x.Id, Name = x.Name }).ToListAsync(cancellationToken)
        };
    }

    private IQueryable<PaymentRequest> LoadDetailQuery()
    {
        return context.PaymentRequests
            .Include(x => x.Department)
            .Include(x => x.Requester)
            .Include(x => x.Vendor)
            .Include(x => x.Budget)
            .Include(x => x.Category)
            .Include(x => x.Items);
    }

    private async Task<Employee> GetOrCreateRequesterAsync(string userId, CancellationToken cancellationToken)
    {
        var userGuid = ParseUserGuid(userId);
        var existing = await context.Employees.SingleOrDefaultAsync(x => x.UserId == userGuid, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var company = await context.Companies.OrderBy(x => x.CreatedAt).FirstAsync(cancellationToken);
        var department = await context.Departments.Where(x => x.CompanyId == company.Id).OrderBy(x => x.Name).FirstAsync(cancellationToken);
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        var nextCode = $"EMP-{await context.Employees.CountAsync(cancellationToken) + 1:0000}";
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            DepartmentId = department.Id,
            UserId = userGuid,
            EmployeeCode = nextCode,
            FullName = user?.Email ?? "Current User",
            Email = user?.Email ?? "unknown@omnibiz.local",
            CreatedAt = DateTime.UtcNow
        };

        context.Employees.Add(employee);
        await context.SaveChangesAsync(cancellationToken);

        return employee;
    }

    private async Task ValidateReferencesAsync(Guid departmentId, Guid categoryId, Guid? vendorId, Guid? budgetId, CancellationToken cancellationToken)
    {
        if (!await context.Departments.AnyAsync(x => x.Id == departmentId, cancellationToken))
        {
            throw new InvalidOperationException("Phòng ban không hợp lệ.");
        }

        if (!await context.BudgetCategories.AnyAsync(x => x.Id == categoryId && x.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("Danh mục ngân sách không hợp lệ.");
        }

        if (vendorId.HasValue && !await context.Vendors.AnyAsync(x => x.Id == vendorId.Value && x.Status == "Active", cancellationToken))
        {
            throw new InvalidOperationException("Nhà cung cấp không hợp lệ.");
        }

        if (budgetId.HasValue && !await context.Budgets.AnyAsync(x => x.Id == budgetId.Value && x.Status == "Active", cancellationToken))
        {
            throw new InvalidOperationException("Ngân sách không hợp lệ.");
        }
    }

    private static void ValidateHeader(string title, string priority, DateOnly? paymentDueDate)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length is < 3 or > 300)
        {
            throw new InvalidOperationException("Tiêu đề phải từ 3 đến 300 ký tự.");
        }

        if (!AllowedPriorities.Contains(priority))
        {
            throw new InvalidOperationException("Mức ưu tiên không hợp lệ.");
        }

        if (paymentDueDate.HasValue && paymentDueDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            throw new InvalidOperationException("Ngày cần thanh toán không được ở quá khứ.");
        }
    }

    private static IReadOnlyList<PaymentRequestItemRequest> NormalizeAndValidate(IEnumerable<PaymentRequestItemRequest> items)
    {
        var normalized = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Description) || x.Quantity != 0 || x.UnitPrice != 0 || x.TaxRate != 0)
            .Select(x => new PaymentRequestItemRequest
            {
                Description = x.Description.Trim(),
                Quantity = x.Quantity,
                Unit = string.IsNullOrWhiteSpace(x.Unit) ? "Item" : x.Unit.Trim(),
                UnitPrice = x.UnitPrice,
                TaxRate = x.TaxRate
            })
            .ToList();

        if (normalized.Count == 0)
        {
            throw new InvalidOperationException("Cần ít nhất một dòng chi tiết.");
        }

        foreach (var item in normalized)
        {
            if (item.Description.Length is < 3 or > 500)
            {
                throw new InvalidOperationException("Mô tả dòng chi tiết phải từ 3 đến 500 ký tự.");
            }

            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("Số lượng phải lớn hơn 0.");
            }

            if (item.UnitPrice < 0)
            {
                throw new InvalidOperationException("Đơn giá không được âm.");
            }

            if (item.TaxRate is < 0 or > 20)
            {
                throw new InvalidOperationException("Thuế suất phải từ 0 đến 20%.");
            }
        }

        return normalized;
    }

    private static void ApplyItems(PaymentRequest entity, IReadOnlyList<PaymentRequestItemRequest> items)
    {
        var sortOrder = 0;
        foreach (var item in items)
        {
            var subtotal = item.Quantity * item.UnitPrice;
            var taxAmount = Math.Round(subtotal * item.TaxRate / 100, 2);
            entity.Items.Add(new PaymentRequestItem
            {
                Id = Guid.NewGuid(),
                Description = item.Description,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                TaxRate = item.TaxRate,
                TaxAmount = taxAmount,
                TotalPrice = subtotal + taxAmount,
                SortOrder = sortOrder++
            });
        }

        entity.TotalAmount = entity.Items.Sum(x => x.TotalPrice);
    }

    private async Task<string> GenerateRequestNumberAsync(int year, CancellationToken cancellationToken)
    {
        var prefix = $"PR-{year}-";
        var count = await context.PaymentRequests.CountAsync(x => x.RequestNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:0000}";
    }

    private static string NormalizePriority(string priority)
    {
        return AllowedPriorities.First(x => string.Equals(x, priority, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static PaymentRequestDetailDto ToDetailDto(PaymentRequest entity)
    {
        return new PaymentRequestDetailDto
        {
            Id = entity.Id,
            RequestNumber = entity.RequestNumber,
            Title = entity.Title,
            Description = entity.Description,
            DepartmentId = entity.DepartmentId,
            DepartmentName = entity.Department?.Name ?? string.Empty,
            RequesterId = entity.RequesterId,
            RequesterName = entity.Requester?.FullName ?? string.Empty,
            VendorId = entity.VendorId,
            VendorName = entity.Vendor?.Name,
            BudgetId = entity.BudgetId,
            BudgetName = entity.Budget?.Name,
            CategoryId = entity.CategoryId,
            CategoryName = entity.Category?.Name ?? string.Empty,
            PaymentDueDate = null,
            TotalAmount = entity.TotalAmount,
            Currency = entity.Currency,
            Priority = entity.Priority,
            Status = entity.Status.ToString(),
            Items = entity.Items.OrderBy(x => x.SortOrder).Select(x => new PaymentRequestItemDto
            {
                Description = x.Description,
                Quantity = x.Quantity,
                Unit = x.Unit,
                UnitPrice = x.UnitPrice,
                TaxRate = x.TaxRate,
                TaxAmount = x.TaxAmount,
                TotalPrice = x.TotalPrice
            }).ToList(),
            CreatedAt = entity.CreatedAt,
            SubmittedAt = entity.SubmittedAt,
            ApprovedAt = entity.ApprovedAt
        };
    }

    private static Guid ParseUserGuid(string userId)
    {
        if (!Guid.TryParse(userId, out var result))
        {
            throw new InvalidOperationException("User id hiện tại không đúng định dạng GUID để liên kết Employee.");
        }

        return result;
    }
}
