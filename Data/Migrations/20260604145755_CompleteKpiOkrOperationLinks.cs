using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompleteKpiOkrOperationLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OperationPlans_KpiDefinitionId",
                table: "OperationPlans");

            migrationBuilder.AddColumn<Guid>(
                name: "OperationRequestId",
                table: "OkrObjectives",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationPlans_KpiDefinitionId",
                table: "OperationPlans",
                column: "KpiDefinitionId",
                unique: true,
                filter: "[KpiDefinitionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OkrObjectives_OperationRequestId",
                table: "OkrObjectives",
                column: "OperationRequestId",
                unique: true,
                filter: "[OperationRequestId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_OkrObjectives_OperationRequests_OperationRequestId",
                table: "OkrObjectives",
                column: "OperationRequestId",
                principalTable: "OperationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OkrObjectives_OperationRequests_OperationRequestId",
                table: "OkrObjectives");

            migrationBuilder.DropIndex(
                name: "IX_OperationPlans_KpiDefinitionId",
                table: "OperationPlans");

            migrationBuilder.DropIndex(
                name: "IX_OkrObjectives_OperationRequestId",
                table: "OkrObjectives");

            migrationBuilder.DropColumn(
                name: "OperationRequestId",
                table: "OkrObjectives");

            migrationBuilder.CreateIndex(
                name: "IX_OperationPlans_KpiDefinitionId",
                table: "OperationPlans",
                column: "KpiDefinitionId");
        }
    }
}
