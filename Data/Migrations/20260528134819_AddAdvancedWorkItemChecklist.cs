using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedWorkItemChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItemChecklists_TenantId",
                table: "WorkItemChecklists");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "WorkItemChecklists",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "WorkItemChecklists",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemChecklists_AssignedToUserId",
                table: "WorkItemChecklists",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemChecklists_TenantId_AssignedToUserId_DueDate",
                table: "WorkItemChecklists",
                columns: new[] { "TenantId", "AssignedToUserId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemChecklists_TenantId_WorkItemId_SortOrder",
                table: "WorkItemChecklists",
                columns: new[] { "TenantId", "WorkItemId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_WorkItemChecklists_AppUsers_AssignedToUserId",
                table: "WorkItemChecklists",
                column: "AssignedToUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkItemChecklists_AppUsers_AssignedToUserId",
                table: "WorkItemChecklists");

            migrationBuilder.DropIndex(
                name: "IX_WorkItemChecklists_AssignedToUserId",
                table: "WorkItemChecklists");

            migrationBuilder.DropIndex(
                name: "IX_WorkItemChecklists_TenantId_AssignedToUserId_DueDate",
                table: "WorkItemChecklists");

            migrationBuilder.DropIndex(
                name: "IX_WorkItemChecklists_TenantId_WorkItemId_SortOrder",
                table: "WorkItemChecklists");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "WorkItemChecklists");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "WorkItemChecklists");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemChecklists_TenantId",
                table: "WorkItemChecklists",
                column: "TenantId");
        }
    }
}
