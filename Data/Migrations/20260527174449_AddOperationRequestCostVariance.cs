using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationRequestCostVariance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_TenantId",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssues_TenantId",
                table: "GoodsIssues");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssueLines_TenantId",
                table: "GoodsIssueLines");

            migrationBuilder.AddColumn<Guid>(
                name: "OperationRequestId",
                table: "PaymentRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualCost",
                table: "OperationRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostVariance",
                table: "OperationRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CostVarianceCalculatedAt",
                table: "OperationRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostVariancePercent",
                table: "OperationRequests",
                type: "decimal(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCost",
                table: "OperationRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LineAmount",
                table: "GoodsIssueLines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "GoodsIssueLines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_OperationRequestId",
                table: "PaymentRequests",
                column: "OperationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_TenantId_OperationRequestId",
                table: "PaymentRequests",
                columns: new[] { "TenantId", "OperationRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequests_TenantId_CostVariancePercent",
                table: "OperationRequests",
                columns: new[] { "TenantId", "CostVariancePercent" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_TenantId_OperationRequestId_Status",
                table: "GoodsIssues",
                columns: new[] { "TenantId", "OperationRequestId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueLines_TenantId_GoodsIssueId",
                table: "GoodsIssueLines",
                columns: new[] { "TenantId", "GoodsIssueId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRequests_OperationRequests_OperationRequestId",
                table: "PaymentRequests",
                column: "OperationRequestId",
                principalTable: "OperationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRequests_OperationRequests_OperationRequestId",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_OperationRequestId",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_TenantId_OperationRequestId",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_OperationRequests_TenantId_CostVariancePercent",
                table: "OperationRequests");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssues_TenantId_OperationRequestId_Status",
                table: "GoodsIssues");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssueLines_TenantId_GoodsIssueId",
                table: "GoodsIssueLines");

            migrationBuilder.DropColumn(
                name: "OperationRequestId",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "ActualCost",
                table: "OperationRequests");

            migrationBuilder.DropColumn(
                name: "CostVariance",
                table: "OperationRequests");

            migrationBuilder.DropColumn(
                name: "CostVarianceCalculatedAt",
                table: "OperationRequests");

            migrationBuilder.DropColumn(
                name: "CostVariancePercent",
                table: "OperationRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                table: "OperationRequests");

            migrationBuilder.DropColumn(
                name: "LineAmount",
                table: "GoodsIssueLines");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "GoodsIssueLines");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_TenantId",
                table: "PaymentRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_TenantId",
                table: "GoodsIssues",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueLines_TenantId",
                table: "GoodsIssueLines",
                column: "TenantId");
        }
    }
}
