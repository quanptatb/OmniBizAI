using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class OrderManagementService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public OrderManagementService(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<SalesOrderIndexViewModel> GetIndexDataAsync(string? search, string? statusFilter)
    {
        var tid = _tenant.TenantId;
        var query = _db.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.WorkflowInstance)
            .Where(o => o.TenantId == tid && !o.IsDeleted);

        // Stats (before filters)
        var total = await query.CountAsync();
        var pendingApproval = await query.CountAsync(o => o.Status == SalesOrderStatus.Submitted);
        var inProduction = await query.CountAsync(o => o.Status == SalesOrderStatus.InProduction);
        var completed = await query.CountAsync(o => o.Status == SalesOrderStatus.Completed);

        // Apply search
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => o.OrderNo.Contains(search) || 
                                     (o.Notes != null && o.Notes.Contains(search)) || 
                                     (o.Customer != null && o.Customer.Name.Contains(search)));
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<SalesOrderStatus>(statusFilter, out var stat))
        {
            query = query.Where(o => o.Status == stat);
        }

        var list = await query.OrderByDescending(o => o.CreatedAt)
            .Select(o => new SalesOrderSummaryViewModel
            {
                Id = o.Id,
                OrderNo = o.OrderNo,
                CustomerName = o.Customer != null ? o.Customer.Name : "N/A",
                OrderDate = o.OrderDate,
                DeliveryDate = o.DeliveryDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                Notes = o.Notes,
                WorkflowStatus = o.WorkflowInstance != null ? o.WorkflowInstance.Status.ToString() : null
            }).ToListAsync();

        return new SalesOrderIndexViewModel
        {
            Orders = list,
            TotalOrders = total,
            PendingApprovalCount = pendingApproval,
            InProductionCount = inProduction,
            CompletedCount = completed,
            Search = search,
            StatusFilter = statusFilter
        };
    }

    public async Task<SalesOrderDetailViewModel?> GetDetailAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var order = await _db.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.WorkflowInstance)
            .Include(o => o.Lines).ThenInclude(l => l.ProductService)
            .Include(o => o.ProductionSteps).ThenInclude(s => s.AssignedUser)
            .Include(o => o.ProductionSteps).ThenInclude(s => s.QcUser)
            .Include(o => o.Traceabilities).ThenInclude(t => t.ProductService)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tid && !o.IsDeleted);

        if (order == null) return null;

        // Fetch approval history
        var approvalTasks = await _db.ApprovalTasks
            .Include(t => t.AssignedToUser)
            .Where(t => t.TenantId == tid && t.TargetType == "SalesOrder" && t.TargetId == id && !t.IsDeleted)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new ApprovalStepItemViewModel
            {
                Id = t.Id,
                StepName = t.StepCode == "DEPARTMENT_REVIEW" ? "Trưởng bộ phận duyệt" : "Ban giám đốc duyệt",
                Status = t.Status.ToString(),
                AssignedToName = t.AssignedToUser != null ? t.AssignedToUser.FullName : null,
                AssignedRole = t.AssignedRole,
                DecisionNote = t.DecisionNote,
                DecidedAt = t.DecidedAt,
                CreatedAt = t.CreatedAt
            }).ToListAsync();

        return new SalesOrderDetailViewModel
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer != null ? order.Customer.Name : "N/A",
            OrderDate = order.OrderDate,
            DeliveryDate = order.DeliveryDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Notes = order.Notes,
            WorkflowInstanceId = order.WorkflowInstanceId,
            WorkflowStatus = order.WorkflowInstance != null ? order.WorkflowInstance.Status.ToString() : null,
            Lines = order.Lines.Select(l => new SalesOrderLineViewModel
            {
                Id = l.Id,
                ProductServiceId = l.ProductServiceId,
                ProductName = l.ProductService != null ? l.ProductService.Name : "N/A",
                ProductCode = l.ProductService != null ? l.ProductService.Code : string.Empty,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                TotalPrice = l.TotalPrice
            }).ToList(),
            ProductionSteps = order.ProductionSteps.OrderBy(s => s.Sequence).Select(s => new ProductionStepViewModel
            {
                Id = s.Id,
                StepName = s.StepName,
                Sequence = s.Sequence,
                Status = s.Status,
                AssignedUserId = s.AssignedUserId,
                AssignedUserName = s.AssignedUser != null ? s.AssignedUser.FullName : null,
                QcStatus = s.QcStatus,
                QcNotes = s.QcNotes,
                QcUserId = s.QcUserId,
                QcUserName = s.QcUser != null ? s.QcUser.FullName : null,
                QcCheckedAt = s.QcCheckedAt
            }).ToList(),
            Traceabilities = order.Traceabilities.OrderByDescending(t => t.CreatedAt).Select(t => new ProductTraceabilityViewModel
            {
                Id = t.Id,
                BatchNo = t.BatchNo,
                ProductName = t.ProductService != null ? t.ProductService.Name : "N/A",
                OriginDetails = t.OriginDetails,
                CreatedAt = t.CreatedAt
            }).ToList(),
            ApprovalHistory = approvalTasks
        };
    }

    public async Task<SalesOrderCreateViewModel> GetCreateFormAsync()
    {
        var tid = _tenant.TenantId;
        var customers = await _db.Customers.Where(c => c.TenantId == tid && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name })
            .ToListAsync();

        var products = await _db.ProductServices.Where(p => p.TenantId == tid && !p.IsDeleted && p.Type == "Product" && p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new SelectOption { Value = p.Id.ToString(), Text = $"{p.Name} ({p.Code}) - {p.StandardPrice:N0} VND" })
            .ToListAsync();

        return new SalesOrderCreateViewModel
        {
            Customers = customers,
            Products = products,
            Lines = new List<SalesOrderLineInputViewModel> { new() }
        };
    }

    public async Task<Guid> CreateAsync(SalesOrderCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        
        // Generate auto OrderNo
        var count = await _db.SalesOrders.CountAsync(o => o.TenantId == tid && o.OrderDate == vm.OrderDate) + 1;
        var dateStr = vm.OrderDate.ToString("yyyyMMdd");
        var orderNo = $"SO-{dateStr}-{count:D3}";

        var order = new SalesOrder
        {
            TenantId = tid,
            OrderNo = orderNo,
            CustomerId = vm.CustomerId,
            OrderDate = vm.OrderDate,
            DeliveryDate = vm.DeliveryDate,
            Notes = vm.Notes,
            Status = SalesOrderStatus.Draft,
            TotalAmount = 0,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        decimal totalAmount = 0;
        foreach (var line in vm.Lines.Where(l => l.ProductServiceId != Guid.Empty))
        {
            var product = await _db.ProductServices.FindAsync(line.ProductServiceId);
            var unitPrice = line.UnitPrice > 0 ? line.UnitPrice : (product?.StandardPrice ?? 0);
            var totalPrice = line.Quantity * unitPrice;
            totalAmount += totalPrice;

            order.Lines.Add(new SalesOrderLine
            {
                TenantId = tid,
                ProductServiceId = line.ProductServiceId,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = totalPrice,
                CreatedByUserId = _tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        order.TotalAmount = totalAmount;
        _db.SalesOrders.Add(order);
        await _db.SaveChangesAsync();

        return order.Id;
    }

    public async Task<bool> SubmitForApprovalAsync(Guid orderId)
    {
        var tid = _tenant.TenantId;
        var order = await _db.SalesOrders.FindAsync(orderId);
        if (order == null || order.TenantId != tid || order.Status != SalesOrderStatus.Draft) return false;

        // Auto get or create a default WorkflowDefinition
        var def = await _db.WorkflowDefinitions.FirstOrDefaultAsync(d => d.TenantId == tid && d.Name == "Quy trình phê duyệt đơn hàng");
        if (def == null)
        {
            def = new WorkflowDefinition
            {
                TenantId = tid,
                Code = "SALES_ORDER_WF",
                Name = "Quy trình phê duyệt đơn hàng",
                TargetEntityName = "SalesOrder",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.WorkflowDefinitions.Add(def);
            await _db.SaveChangesAsync();
        }

        var workflowInstance = new WorkflowInstance
        {
            TenantId = tid,
            WorkflowDefinitionId = def.Id,
            EntityName = "SalesOrder",
            EntityId = order.Id,
            Status = WorkflowInstanceStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.WorkflowInstances.Add(workflowInstance);
        await _db.SaveChangesAsync();

        order.WorkflowInstanceId = workflowInstance.Id;
        order.Status = SalesOrderStatus.Submitted;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        // Create Level 1 Department Review Task
        var task = new ApprovalTask
        {
            TenantId = tid,
            WorkflowInstanceId = workflowInstance.Id,
            TargetType = "SalesOrder",
            TargetId = order.Id,
            StepCode = "DEPARTMENT_REVIEW",
            AssignedRole = "DEPARTMENT_MANAGER",
            Status = ApprovalStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.ApprovalTasks.Add(task);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> StartProductionAsync(Guid orderId)
    {
        var tid = _tenant.TenantId;
        var order = await _db.SalesOrders
            .Include(o => o.Lines).ThenInclude(l => l.ProductService)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tid && !o.IsDeleted);

        if (order == null || order.Status != SalesOrderStatus.Approved) return false;

        order.Status = SalesOrderStatus.InProduction;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        // Check if steps already exist, if not, create them
        var hasSteps = await _db.ProductionSteps.AnyAsync(s => s.SalesOrderId == orderId && s.TenantId == tid && !s.IsDeleted);
        if (!hasSteps)
        {
            var steps = new List<string> {
                "Chuẩn bị nguyên vật liệu",
                "Gia công chế tạo",
                "Lắp ráp thành phẩm",
                "Kiểm định chất lượng QA/QC",
                "Đóng gói & Bàn giao"
            };

            for (int i = 0; i < steps.Count; i++)
            {
                var pStep = new ProductionStep
                {
                    TenantId = tid,
                    SalesOrderId = order.Id,
                    StepName = steps[i],
                    Sequence = i + 1,
                    Status = ProductionStepStatus.Todo,
                    QcStatus = QcStatus.Pending,
                    CreatedByUserId = _tenant.UserId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.ProductionSteps.Add(pStep);
            }
        }

        // Initialize ProductTraceability for each line item
        foreach (var line in order.Lines)
        {
            var batchNo = $"LOT-{order.OrderNo}-{line.ProductService?.Code ?? "PROD"}";
            var trace = new ProductTraceability
            {
                TenantId = tid,
                SalesOrderId = order.Id,
                ProductServiceId = line.ProductServiceId,
                BatchNo = batchNo,
                OriginDetails = $"[Khởi động sản xuất] Khởi tạo quy trình sản xuất lô hàng {batchNo} cho sản phẩm '{line.ProductService?.Name ?? "N/A"}' (Số lượng: {line.Quantity:N0}). Khởi chạy các công đoạn sản xuất & chất lượng vận hành.",
                CreatedByUserId = _tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.ProductTraceabilities.Add(trace);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateProductionStepAsync(Guid stepId, ProductionStepStatus status, Guid? assignedUserId)
    {
        var tid = _tenant.TenantId;
        var step = await _db.ProductionSteps
            .Include(s => s.SalesOrder)
            .FirstOrDefaultAsync(s => s.Id == stepId && s.TenantId == tid && !s.IsDeleted);

        if (step == null) return false;

        var oldStatus = step.Status;
        step.Status = status;
        if (assignedUserId.HasValue) step.AssignedUserId = assignedUserId;
        step.UpdatedAt = DateTimeOffset.UtcNow;

        // Log to product traceability
        var order = step.SalesOrder;
        if (order != null)
        {
            var primaryLine = await _db.SalesOrderLines.FirstOrDefaultAsync(l => l.SalesOrderId == order.Id);
            var prodId = primaryLine?.ProductServiceId ?? Guid.Empty;
            
            var assignedUser = assignedUserId.HasValue ? await _db.AppUsers.FindAsync(assignedUserId) : null;
            var userName = assignedUser != null ? assignedUser.FullName : _tenant.UserFullName;

            var statusStr = status switch
            {
                ProductionStepStatus.Todo => "Chờ làm (Todo)",
                ProductionStepStatus.InProgress => "Đang xử lý (In Progress)",
                ProductionStepStatus.Completed => "Hoàn thành (Completed)",
                _ => status.ToString()
            };

            var trace = new ProductTraceability
            {
                TenantId = tid,
                SalesOrderId = order.Id,
                ProductServiceId = prodId,
                BatchNo = $"LOT-{order.OrderNo}",
                OriginDetails = $"[Vận hành công đoạn] Công đoạn '{step.StepName}' (Bước {step.Sequence}) chuyển từ trạng thái {oldStatus} sang {statusStr}. Thực hiện bởi: {userName}.",
                CreatedByUserId = _tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.ProductTraceabilities.Add(trace);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SubmitQcAsync(SalesOrderQcInputViewModel vm)
    {
        var tid = _tenant.TenantId;
        var step = await _db.ProductionSteps
            .Include(s => s.SalesOrder).ThenInclude(o => o!.Lines)
            .FirstOrDefaultAsync(s => s.Id == vm.ProductionStepId && s.TenantId == tid && !s.IsDeleted);

        if (step == null) return false;

        step.QcStatus = vm.QcStatus;
        step.QcNotes = vm.QcNotes;
        step.QcUserId = _tenant.UserId;
        step.QcCheckedAt = DateTimeOffset.UtcNow;
        step.UpdatedAt = DateTimeOffset.UtcNow;

        var order = step.SalesOrder;
        if (order != null)
        {
            var primaryLine = order.Lines.FirstOrDefault();
            var prodId = primaryLine?.ProductServiceId ?? Guid.Empty;

            var qcStatusStr = vm.QcStatus switch
            {
                QcStatus.Passed => "ĐẠT CHẤT LƯỢNG (Passed)",
                QcStatus.Failed => "KHÔNG ĐẠT CHẤT LƯỢNG (Failed)",
                _ => vm.QcStatus.ToString()
            };

            // Log to Product Traceability
            var trace = new ProductTraceability
            {
                TenantId = tid,
                SalesOrderId = order.Id,
                ProductServiceId = prodId,
                BatchNo = $"LOT-{order.OrderNo}",
                OriginDetails = $"[Đánh giá QA/QC] Công đoạn '{step.StepName}' đã được thẩm định chất lượng. Kết quả: {qcStatusStr}. Chuyên viên kiểm định: {_tenant.UserFullName}. Ghi chú: {vm.QcNotes ?? "N/A"}",
                CreatedByUserId = _tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.ProductTraceabilities.Add(trace);

            // Handle QC Logic
            if (vm.QcStatus == QcStatus.Passed)
            {
                // If it was Completed status and QC passed, we can keep it completed.
                // Let's check if ALL production steps are Completed AND passed QC
                var allSteps = await _db.ProductionSteps.Where(s => s.SalesOrderId == order.Id && s.TenantId == tid && !s.IsDeleted).ToListAsync();
                if (allSteps.All(s => s.Status == ProductionStepStatus.Completed && s.QcStatus == QcStatus.Passed))
                {
                    // Order completed!
                    order.Status = SalesOrderStatus.Completed;
                    order.UpdatedAt = DateTimeOffset.UtcNow;

                    var completionTrace = new ProductTraceability
                    {
                        TenantId = tid,
                        SalesOrderId = order.Id,
                        ProductServiceId = prodId,
                        BatchNo = $"LOT-{order.OrderNo}",
                        OriginDetails = $"[Hoàn thành xuất xưởng] Đơn hàng {order.OrderNo} đã vượt qua toàn bộ 5 công đoạn kiểm định QA/QC nghiêm ngặt. Trạng thái đơn hàng cập nhật thành HOÀN THÀNH. Sản phẩm đạt chuẩn Apple Design System và sẵn sàng bàn giao khách hàng.",
                        CreatedByUserId = _tenant.UserId,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _db.ProductTraceabilities.Add(completionTrace);
                }
            }
            else if (vm.QcStatus == QcStatus.Failed)
            {
                // QC failed: Reset production step back to Todo so workers can fix the issues.
                step.Status = ProductionStepStatus.Todo;
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<SelectOption>> GetUsersAsync()
    {
        return await _db.AppUsers
            .Where(u => u.TenantId == _tenant.TenantId && !u.IsDeleted && u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
            .ToListAsync();
    }
}
