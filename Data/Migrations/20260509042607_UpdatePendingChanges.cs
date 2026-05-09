using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "KanbanColumnId",
                table: "WorkItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KanbanColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccentColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDoneColumn = table.Column<bool>(type: "bit", nullable: false),
                    IsCancelledColumn = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanColumns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_KanbanColumnId",
                table: "WorkItems",
                column: "KanbanColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_TenantId_KanbanColumnId",
                table: "WorkItems",
                columns: new[] { "TenantId", "KanbanColumnId" });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanColumns_TenantId_SortOrder",
                table: "KanbanColumns",
                columns: new[] { "TenantId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_WorkItems_KanbanColumns_KanbanColumnId",
                table: "WorkItems",
                column: "KanbanColumnId",
                principalTable: "KanbanColumns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkItems_KanbanColumns_KanbanColumnId",
                table: "WorkItems");

            migrationBuilder.DropTable(
                name: "KanbanColumns");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_KanbanColumnId",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_TenantId_KanbanColumnId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "KanbanColumnId",
                table: "WorkItems");
        }
    }
}
