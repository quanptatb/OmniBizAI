using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationRequestSlaPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovalDueAt",
                table: "OperationRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "OperationRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolutionDueAt",
                table: "OperationRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAt",
                table: "OperationRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OperationSlaBreaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BreachType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HoursOverdue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    NotificationSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationSlaBreaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationSlaBreaches_OperationRequests_OperationRequestId",
                        column: x => x.OperationRequestId,
                        principalTable: "OperationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationSlaBreaches_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperationSlaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaxApprovalHours = table.Column<int>(type: "int", nullable: false),
                    MaxResolutionHours = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationSlaPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationSlaPolicies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationSlaBreaches_OperationRequestId",
                table: "OperationSlaBreaches",
                column: "OperationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationSlaBreaches_TenantId_OperationRequestId_BreachType",
                table: "OperationSlaBreaches",
                columns: new[] { "TenantId", "OperationRequestId", "BreachType" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OperationSlaPolicies_TenantId_Priority",
                table: "OperationSlaPolicies",
                columns: new[] { "TenantId", "Priority" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationSlaBreaches");

            migrationBuilder.DropTable(
                name: "OperationSlaPolicies");

            migrationBuilder.DropColumn(
                name: "ApprovalDueAt",
                table: "OperationRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "OperationRequests");

            migrationBuilder.DropColumn(
                name: "ResolutionDueAt",
                table: "OperationRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "OperationRequests");
        }
    }
}
