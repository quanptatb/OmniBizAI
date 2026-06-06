using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationProgressLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationProgressLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationProgressLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationProgressLogs_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationProgressLogs_OperationRequests_OperationRequestId",
                        column: x => x.OperationRequestId,
                        principalTable: "OperationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationProgressLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationProgressLogs_CreatedByUserId",
                table: "OperationProgressLogs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationProgressLogs_OperationRequestId",
                table: "OperationProgressLogs",
                column: "OperationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationProgressLogs_TenantId_OperationRequestId_CreatedAt",
                table: "OperationProgressLogs",
                columns: new[] { "TenantId", "OperationRequestId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationProgressLogs");
        }
    }
}
