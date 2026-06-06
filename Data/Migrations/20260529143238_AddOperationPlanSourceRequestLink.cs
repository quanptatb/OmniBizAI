using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationPlanSourceRequestLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceOperationRequestId",
                table: "OperationPlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationPlans_SourceOperationRequestId",
                table: "OperationPlans",
                column: "SourceOperationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationPlans_TenantId_SourceOperationRequestId",
                table: "OperationPlans",
                columns: new[] { "TenantId", "SourceOperationRequestId" },
                unique: true,
                filter: "[SourceOperationRequestId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_OperationPlans_OperationRequests_SourceOperationRequestId",
                table: "OperationPlans",
                column: "SourceOperationRequestId",
                principalTable: "OperationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperationPlans_OperationRequests_SourceOperationRequestId",
                table: "OperationPlans");

            migrationBuilder.DropIndex(
                name: "IX_OperationPlans_SourceOperationRequestId",
                table: "OperationPlans");

            migrationBuilder.DropIndex(
                name: "IX_OperationPlans_TenantId_SourceOperationRequestId",
                table: "OperationPlans");

            migrationBuilder.DropColumn(
                name: "SourceOperationRequestId",
                table: "OperationPlans");
        }
    }
}
