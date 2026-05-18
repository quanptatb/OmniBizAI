using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequests_Dashboard_CreatedAt",
                table: "OperationRequests",
                columns: new[] { "TenantId", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequests_Dashboard_OrganizationUnit",
                table: "OperationRequests",
                columns: new[] { "TenantId", "IsDeleted", "OrganizationUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequests_Dashboard_Status_DueDate",
                table: "OperationRequests",
                columns: new[] { "TenantId", "IsDeleted", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequests_Dashboard_Status_UpdatedAt",
                table: "OperationRequests",
                columns: new[] { "TenantId", "IsDeleted", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Dashboard_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Dashboard_Status",
                table: "AppUsers",
                columns: new[] { "TenantId", "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalTasks_Dashboard_Status",
                table: "ApprovalTasks",
                columns: new[] { "TenantId", "IsDeleted", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OperationRequests_Dashboard_CreatedAt",
                table: "OperationRequests");

            migrationBuilder.DropIndex(
                name: "IX_OperationRequests_Dashboard_OrganizationUnit",
                table: "OperationRequests");

            migrationBuilder.DropIndex(
                name: "IX_OperationRequests_Dashboard_Status_DueDate",
                table: "OperationRequests");

            migrationBuilder.DropIndex(
                name: "IX_OperationRequests_Dashboard_Status_UpdatedAt",
                table: "OperationRequests");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Dashboard_CreatedAt",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_Dashboard_Status",
                table: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalTasks_Dashboard_Status",
                table: "ApprovalTasks");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");
        }
    }
}
