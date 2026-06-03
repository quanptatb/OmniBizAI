using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemActivityAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkItemActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromColumnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToColumnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkItemActivities_AppUsers_MovedByUserId",
                        column: x => x.MovedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkItemActivities_KanbanColumns_FromColumnId",
                        column: x => x.FromColumnId,
                        principalTable: "KanbanColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkItemActivities_KanbanColumns_ToColumnId",
                        column: x => x.ToColumnId,
                        principalTable: "KanbanColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkItemActivities_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkItemActivities_WorkItems_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "WorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemActivities_FromColumnId",
                table: "WorkItemActivities",
                column: "FromColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemActivities_MovedByUserId",
                table: "WorkItemActivities",
                column: "MovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemActivities_TenantId_ToColumnId_MovedAt",
                table: "WorkItemActivities",
                columns: new[] { "TenantId", "ToColumnId", "MovedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemActivities_TenantId_WorkItemId_MovedAt",
                table: "WorkItemActivities",
                columns: new[] { "TenantId", "WorkItemId", "MovedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemActivities_ToColumnId",
                table: "WorkItemActivities",
                column: "ToColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemActivities_WorkItemId",
                table: "WorkItemActivities",
                column: "WorkItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkItemActivities");
        }
    }
}
