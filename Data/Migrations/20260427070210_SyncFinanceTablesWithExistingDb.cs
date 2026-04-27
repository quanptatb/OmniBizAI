using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncFinanceTablesWithExistingDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // MIGRATION NÀY LÀ EMPTY ON PURPOSE
            // ============================================================
            // Tất cả bảng (Companies, Departments, FiscalPeriods, BudgetCategories, budgets)
            // đã được tạo sẵn trong database bởi migration schema ngoài EF tracking
            // (conversation 947c0b74 — Finance Module Entity Implementation).
            //
            // Migration này chỉ để đồng bộ EF model snapshot với DB hiện tại,
            // KHÔNG tạo thêm bảng hay cột nào. Chạy migration này sẽ chỉ insert
            // record vào __EFMigrationsHistory để EF biết model đã được sync.
            // ============================================================
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Empty — không rollback vì Up() không thay đổi gì
        }
    }
}
