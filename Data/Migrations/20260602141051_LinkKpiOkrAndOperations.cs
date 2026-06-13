using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkKpiOkrAndOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "KpiDefinitionId",
                table: "OperationPlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OkrObjectiveId",
                table: "OperationPlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationRequestId",
                table: "KpiDefinitions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationPlans_KpiDefinitionId",
                table: "OperationPlans",
                column: "KpiDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationPlans_OkrObjectiveId",
                table: "OperationPlans",
                column: "OkrObjectiveId",
                unique: true,
                filter: "[OkrObjectiveId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDefinitions_OperationRequestId",
                table: "KpiDefinitions",
                column: "OperationRequestId",
                unique: true,
                filter: "[OperationRequestId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_KpiDefinitions_OperationRequests_OperationRequestId",
                table: "KpiDefinitions",
                column: "OperationRequestId",
                principalTable: "OperationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OperationPlans_KpiDefinitions_KpiDefinitionId",
                table: "OperationPlans",
                column: "KpiDefinitionId",
                principalTable: "KpiDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OperationPlans_OkrObjectives_OkrObjectiveId",
                table: "OperationPlans",
                column: "OkrObjectiveId",
                principalTable: "OkrObjectives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KpiDefinitions_OperationRequests_OperationRequestId",
                table: "KpiDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_OperationPlans_KpiDefinitions_KpiDefinitionId",
                table: "OperationPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_OperationPlans_OkrObjectives_OkrObjectiveId",
                table: "OperationPlans");

            migrationBuilder.DropIndex(
                name: "IX_OperationPlans_KpiDefinitionId",
                table: "OperationPlans");

            migrationBuilder.DropIndex(
                name: "IX_OperationPlans_OkrObjectiveId",
                table: "OperationPlans");

            migrationBuilder.DropIndex(
                name: "IX_KpiDefinitions_OperationRequestId",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "KpiDefinitionId",
                table: "OperationPlans");

            migrationBuilder.DropColumn(
                name: "OkrObjectiveId",
                table: "OperationPlans");

            migrationBuilder.DropColumn(
                name: "OperationRequestId",
                table: "KpiDefinitions");
        }
    }
}
