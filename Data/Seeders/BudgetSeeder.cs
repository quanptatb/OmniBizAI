using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities.Finance;
using OmniBizAI.Models.Entities.Organization;

namespace OmniBizAI.Data.Seeders;

/// <summary>
/// Seeder dữ liệu ngân sách cho kịch bản demo BAO-02.
/// Blueprint mục 9.5: "Marketing gần vượt ngân sách" — Budget 300M, spent 245M, committed 40M.
/// Blueprint mục 9.7: Seed phải idempotent — chạy nhiều lần không tạo duplicate.
///
/// Kịch bản seed:
/// - Company: OmniBiz Solutions (lấy từ DB nếu đã có)
/// - Department: Marketing (MKT)
/// - FiscalPeriod: Tháng 05/2026
/// - BudgetCategory: MARKETING_ADS
/// - Budget: AllocatedAmount=300M, SpentAmount=245M, CommittedAmount=40M
/// - Kỳ vọng: UtilizationPct = 95%, WarningLevel = "Warning"
/// </summary>
public static class BudgetSeeder
{
    // ========================================
    // GUID cố định để idempotent — chạy lại không tạo duplicate
    // ========================================
    private static readonly Guid DeptMarketingId = new("20000000-0000-0000-0000-000000000005");
    private static readonly Guid FiscalPeriodMay2026Id = new("30000000-0000-0000-0000-000000000005");
    private static readonly Guid CategoryMarketingAdsId = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid BudgetMarketingAdsId = new("50000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Chạy seed dữ liệu ngân sách Marketing.
    /// Idempotent: kiểm tra tồn tại trước khi insert.
    /// </summary>
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Lấy Company đầu tiên trong DB (đã có từ migration cũ)
        var company = await context.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync();

        if (company is null)
        {
            // Nếu chưa có company nào, tạo mới
            company = new Company
            {
                Id = new Guid("10000000-0000-0000-0000-000000000001"),
                Name = "OmniBiz Solutions",
                Code = "OMNIBIZ",
                Currency = "VND",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();
        }

        // ========================================
        // Seed Department — Marketing (MKT)
        // ========================================
        if (!await context.Departments.IgnoreQueryFilters().AnyAsync(d => d.Id == DeptMarketingId))
        {
            context.Departments.Add(new Department
            {
                Id = DeptMarketingId,
                CompanyId = company.Id,
                Code = "MKT",
                Name = "Marketing",
                BudgetLimit = 350_000_000m,
                IsActive = true,
                SortOrder = 5,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // ========================================
        // Seed FiscalPeriod — Tháng 05/2026
        // ========================================
        if (!await context.FiscalPeriods.AnyAsync(fp => fp.Id == FiscalPeriodMay2026Id))
        {
            context.FiscalPeriods.Add(new FiscalPeriod
            {
                Id = FiscalPeriodMay2026Id,
                CompanyId = company.Id,
                Name = "Tháng 05/2026",
                PeriodType = "Monthly",
                StartDate = new DateOnly(2026, 5, 1),
                EndDate = new DateOnly(2026, 5, 31),
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // ========================================
        // Seed BudgetCategory — MARKETING_ADS
        // ========================================
        if (!await context.BudgetCategories.AnyAsync(bc => bc.Id == CategoryMarketingAdsId))
        {
            context.BudgetCategories.Add(new BudgetCategory
            {
                Id = CategoryMarketingAdsId,
                Code = "MARKETING_ADS",
                Name = "Chi phí quảng cáo Marketing",
                Description = "Ngân sách cho chiến dịch quảng cáo, Google Ads, Facebook Ads, ...",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // ========================================
        // Seed Budget — Marketing Ads 05/2026
        // UtilizationPct = (245M + 40M) / 300M * 100 = 95% → WarningLevel = "Warning"
        // ========================================
        if (!await context.Budgets.IgnoreQueryFilters().AnyAsync(b => b.Id == BudgetMarketingAdsId))
        {
            var allocatedAmount = 300_000_000m;
            var spentAmount = 245_000_000m;
            var committedAmount = 40_000_000m;
            var remainingAmount = allocatedAmount - spentAmount - committedAmount; // = 15,000,000
            var utilizationPct = Math.Round((spentAmount + committedAmount) / allocatedAmount * 100m, 2); // = 95.00

            context.Budgets.Add(new Budget
            {
                Id = BudgetMarketingAdsId,
                CompanyId = company.Id,
                FiscalPeriodId = FiscalPeriodMay2026Id,
                DepartmentId = DeptMarketingId,
                CategoryId = CategoryMarketingAdsId,
                Name = "Marketing Ads - 05/2026",
                AllocatedAmount = allocatedAmount,
                SpentAmount = spentAmount,
                CommittedAmount = committedAmount,
                RemainingAmount = remainingAmount,
                UtilizationPct = utilizationPct,
                WarningThreshold = 80m,
                Status = "Active",
                Notes = "Ngân sách quảng cáo Marketing tháng 5/2026. CẢNH BÁO: Utilization đạt 95%.",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }
}
