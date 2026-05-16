using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeKpiOkrSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KpiDefinitions_TenantId",
                table: "KpiDefinitions");

            migrationBuilder.AddColumn<int>(
                name: "CheckInFrequencyDays",
                table: "KpiTargets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DeadlineTime",
                table: "KpiTargets",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FailThreshold",
                table: "KpiTargets",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PassThreshold",
                table: "KpiTargets",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReminderEnabled",
                table: "KpiTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Classification",
                table: "KpiResults",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EvaluationPeriodId",
                table: "KpiResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GradingRankId",
                table: "KpiResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProgressPercent",
                table: "KpiResults",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "KpiDefinitions",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "KpiDefinitions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignerUserId",
                table: "KpiDefinitions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "KpiDefinitions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EvaluationPeriodId",
                table: "KpiDefinitions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MeasureType",
                table: "KpiDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OkrKeyResultId",
                table: "KpiDefinitions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OkrObjectiveId",
                table: "KpiDefinitions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyType",
                table: "KpiDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "KpiDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadlineAt",
                table: "KpiCheckIns",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                table: "KpiCheckIns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "KpiFailReasonId",
                table: "KpiCheckIns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewComment",
                table: "KpiCheckIns",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReviewScore",
                table: "KpiCheckIns",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "KpiCheckIns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "KpiCheckIns",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "KpiCheckIns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "KpiCheckIns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiGenerationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PromptText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiGenerationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiGenerationHistories_AppUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiGenerationHistories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationPeriods_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradingRanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankCode = table.Column<int>(type: "int", nullable: false),
                    RankName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingRanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradingRanks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiAdjustmentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KpiDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdjusterUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldChanged = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiAdjustmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiAdjustmentHistories_AppUsers_AdjusterUserId",
                        column: x => x.AdjusterUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiAdjustmentHistories_KpiDefinitions_KpiDefinitionId",
                        column: x => x.KpiDefinitionId,
                        principalTable: "KpiDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiAdjustmentHistories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiCheckInDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KpiCheckInId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AchievedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiCheckInDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiCheckInDetails_KpiCheckIns_KpiCheckInId",
                        column: x => x.KpiCheckInId,
                        principalTable: "KpiCheckIns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiCheckInDetails_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiCheckInHistoryLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KpiCheckInId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiCheckInHistoryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiCheckInHistoryLogs_KpiCheckIns_KpiCheckInId",
                        column: x => x.KpiCheckInId,
                        principalTable: "KpiCheckIns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiCheckInHistoryLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiDepartmentAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KpiDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiDepartmentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiDepartmentAssignments_KpiDefinitions_KpiDefinitionId",
                        column: x => x.KpiDefinitionId,
                        principalTable: "KpiDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiDepartmentAssignments_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiDepartmentAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiEmployeeAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KpiDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiEmployeeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiEmployeeAssignments_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiEmployeeAssignments_KpiDefinitions_KpiDefinitionId",
                        column: x => x.KpiDefinitionId,
                        principalTable: "KpiDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiEmployeeAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiFailReasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReasonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_KpiFailReasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiFailReasons_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiGoalComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KpiDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    KpiCheckInId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    CommentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiGoalComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiGoalComments_AppUsers_CommenterId",
                        column: x => x.CommenterId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiGoalComments_KpiCheckIns_KpiCheckInId",
                        column: x => x.KpiCheckInId,
                        principalTable: "KpiCheckIns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiGoalComments_KpiDefinitions_KpiDefinitionId",
                        column: x => x.KpiDefinitionId,
                        principalTable: "KpiDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiGoalComments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MissionVisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TargetYear = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FinancialTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                    table.PrimaryKey("PK_MissionVisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionVisions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OkrObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Cycle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_OkrObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkrObjectives_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OneOnOneMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Agenda = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneOnOneMeetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneOnOneMeetings_AppUsers_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OneOnOneMeetings_AppUsers_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OneOnOneMeetings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiResultComparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KpiDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AchievedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompletionPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiResultComparisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiResultComparisons_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiResultComparisons_EvaluationPeriods_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "EvaluationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiResultComparisons_KpiDefinitions_KpiDefinitionId",
                        column: x => x.KpiDefinitionId,
                        principalTable: "KpiDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KpiResultComparisons_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RealtimeExpectedBonuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstimatedBonus = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    EstimatedRank = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealtimeExpectedBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RealtimeExpectedBonuses_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RealtimeExpectedBonuses_EvaluationPeriods_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "EvaluationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RealtimeExpectedBonuses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BonusRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradingRankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalaryPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_BonusRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonusRules_GradingRanks_GradingRankId",
                        column: x => x.GradingRankId,
                        principalTable: "GradingRanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BonusRules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    GradingRankId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReviewComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubmissionStatus = table.Column<int>(type: "int", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DirectorReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DirectorReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DirectorReviewComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_AppUsers_DirectorReviewedByUserId",
                        column: x => x.DirectorReviewedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_AppUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_EvaluationPeriods_EvaluationPeriodId",
                        column: x => x.EvaluationPeriodId,
                        principalTable: "EvaluationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_GradingRanks_GradingRankId",
                        column: x => x.GradingRankId,
                        principalTable: "GradingRanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OkrDepartmentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OkrObjectiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkrDepartmentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkrDepartmentAllocations_OkrObjectives_OkrObjectiveId",
                        column: x => x.OkrObjectiveId,
                        principalTable: "OkrObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OkrDepartmentAllocations_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OkrDepartmentAllocations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OkrEmployeeAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OkrObjectiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkrEmployeeAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkrEmployeeAllocations_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OkrEmployeeAllocations_OkrObjectives_OkrObjectiveId",
                        column: x => x.OkrObjectiveId,
                        principalTable: "OkrObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OkrEmployeeAllocations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OkrKeyResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OkrObjectiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyResultName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsInverse = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkrKeyResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkrKeyResults_OkrObjectives_OkrObjectiveId",
                        column: x => x.OkrObjectiveId,
                        principalTable: "OkrObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OkrKeyResults_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OkrMissionMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OkrObjectiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionVisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OkrMissionMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OkrMissionMappings_MissionVisions_MissionVisionId",
                        column: x => x.MissionVisionId,
                        principalTable: "MissionVisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OkrMissionMappings_OkrObjectives_OkrObjectiveId",
                        column: x => x.OkrObjectiveId,
                        principalTable: "OkrObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OkrMissionMappings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KpiResults_EvaluationPeriodId",
                table: "KpiResults",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiResults_GradingRankId",
                table: "KpiResults",
                column: "GradingRankId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDefinitions_AssignerUserId",
                table: "KpiDefinitions",
                column: "AssignerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDefinitions_EvaluationPeriodId",
                table: "KpiDefinitions",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDefinitions_OkrKeyResultId",
                table: "KpiDefinitions",
                column: "OkrKeyResultId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDefinitions_OkrObjectiveId",
                table: "KpiDefinitions",
                column: "OkrObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDefinitions_TenantId_Code",
                table: "KpiDefinitions",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KpiCheckIns_KpiFailReasonId",
                table: "KpiCheckIns",
                column: "KpiFailReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiCheckIns_ReviewedByUserId",
                table: "KpiCheckIns",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiCheckIns_SubmittedByUserId",
                table: "KpiCheckIns",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiGenerationHistories_RequestedByUserId",
                table: "AiGenerationHistories",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiGenerationHistories_TenantId",
                table: "AiGenerationHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusRules_GradingRankId",
                table: "BonusRules",
                column: "GradingRankId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusRules_TenantId",
                table: "BonusRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationPeriods_TenantId",
                table: "EvaluationPeriods",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_DirectorReviewedByUserId",
                table: "EvaluationResults",
                column: "DirectorReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_EvaluationPeriodId",
                table: "EvaluationResults",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_GradingRankId",
                table: "EvaluationResults",
                column: "GradingRankId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_SubmittedByUserId",
                table: "EvaluationResults",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_TenantId",
                table: "EvaluationResults",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_UserId_EvaluationPeriodId",
                table: "EvaluationResults",
                columns: new[] { "UserId", "EvaluationPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradingRanks_TenantId",
                table: "GradingRanks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiAdjustmentHistories_AdjusterUserId",
                table: "KpiAdjustmentHistories",
                column: "AdjusterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiAdjustmentHistories_KpiDefinitionId",
                table: "KpiAdjustmentHistories",
                column: "KpiDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiAdjustmentHistories_TenantId",
                table: "KpiAdjustmentHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiCheckInDetails_KpiCheckInId",
                table: "KpiCheckInDetails",
                column: "KpiCheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiCheckInDetails_TenantId",
                table: "KpiCheckInDetails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiCheckInHistoryLogs_KpiCheckInId",
                table: "KpiCheckInHistoryLogs",
                column: "KpiCheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiCheckInHistoryLogs_TenantId",
                table: "KpiCheckInHistoryLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDepartmentAssignments_KpiDefinitionId_OrganizationUnitId",
                table: "KpiDepartmentAssignments",
                columns: new[] { "KpiDefinitionId", "OrganizationUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KpiDepartmentAssignments_OrganizationUnitId",
                table: "KpiDepartmentAssignments",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDepartmentAssignments_TenantId",
                table: "KpiDepartmentAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiEmployeeAssignments_KpiDefinitionId_UserId",
                table: "KpiEmployeeAssignments",
                columns: new[] { "KpiDefinitionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KpiEmployeeAssignments_TenantId",
                table: "KpiEmployeeAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiEmployeeAssignments_UserId",
                table: "KpiEmployeeAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiFailReasons_TenantId",
                table: "KpiFailReasons",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiGoalComments_CommenterId",
                table: "KpiGoalComments",
                column: "CommenterId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiGoalComments_KpiCheckInId",
                table: "KpiGoalComments",
                column: "KpiCheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiGoalComments_KpiDefinitionId",
                table: "KpiGoalComments",
                column: "KpiDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiGoalComments_TenantId",
                table: "KpiGoalComments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiResultComparisons_EvaluationPeriodId",
                table: "KpiResultComparisons",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiResultComparisons_KpiDefinitionId",
                table: "KpiResultComparisons",
                column: "KpiDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiResultComparisons_TenantId",
                table: "KpiResultComparisons",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiResultComparisons_UserId",
                table: "KpiResultComparisons",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionVisions_TenantId",
                table: "MissionVisions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OkrDepartmentAllocations_OkrObjectiveId_OrganizationUnitId",
                table: "OkrDepartmentAllocations",
                columns: new[] { "OkrObjectiveId", "OrganizationUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OkrDepartmentAllocations_OrganizationUnitId",
                table: "OkrDepartmentAllocations",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OkrDepartmentAllocations_TenantId",
                table: "OkrDepartmentAllocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OkrEmployeeAllocations_OkrObjectiveId_UserId",
                table: "OkrEmployeeAllocations",
                columns: new[] { "OkrObjectiveId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OkrEmployeeAllocations_TenantId",
                table: "OkrEmployeeAllocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OkrEmployeeAllocations_UserId",
                table: "OkrEmployeeAllocations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OkrKeyResults_OkrObjectiveId",
                table: "OkrKeyResults",
                column: "OkrObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_OkrKeyResults_TenantId",
                table: "OkrKeyResults",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OkrMissionMappings_MissionVisionId",
                table: "OkrMissionMappings",
                column: "MissionVisionId");

            migrationBuilder.CreateIndex(
                name: "IX_OkrMissionMappings_OkrObjectiveId_MissionVisionId",
                table: "OkrMissionMappings",
                columns: new[] { "OkrObjectiveId", "MissionVisionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OkrMissionMappings_TenantId",
                table: "OkrMissionMappings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OkrObjectives_TenantId",
                table: "OkrObjectives",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OneOnOneMeetings_EmployeeUserId",
                table: "OneOnOneMeetings",
                column: "EmployeeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OneOnOneMeetings_ManagerUserId",
                table: "OneOnOneMeetings",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OneOnOneMeetings_TenantId",
                table: "OneOnOneMeetings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeExpectedBonuses_EvaluationPeriodId",
                table: "RealtimeExpectedBonuses",
                column: "EvaluationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeExpectedBonuses_TenantId",
                table: "RealtimeExpectedBonuses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeExpectedBonuses_UserId",
                table: "RealtimeExpectedBonuses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_KpiCheckIns_AppUsers_ReviewedByUserId",
                table: "KpiCheckIns",
                column: "ReviewedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiCheckIns_AppUsers_SubmittedByUserId",
                table: "KpiCheckIns",
                column: "SubmittedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiCheckIns_KpiFailReasons_KpiFailReasonId",
                table: "KpiCheckIns",
                column: "KpiFailReasonId",
                principalTable: "KpiFailReasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiDefinitions_AppUsers_AssignerUserId",
                table: "KpiDefinitions",
                column: "AssignerUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiDefinitions_EvaluationPeriods_EvaluationPeriodId",
                table: "KpiDefinitions",
                column: "EvaluationPeriodId",
                principalTable: "EvaluationPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiDefinitions_OkrKeyResults_OkrKeyResultId",
                table: "KpiDefinitions",
                column: "OkrKeyResultId",
                principalTable: "OkrKeyResults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiDefinitions_OkrObjectives_OkrObjectiveId",
                table: "KpiDefinitions",
                column: "OkrObjectiveId",
                principalTable: "OkrObjectives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiResults_EvaluationPeriods_EvaluationPeriodId",
                table: "KpiResults",
                column: "EvaluationPeriodId",
                principalTable: "EvaluationPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiResults_GradingRanks_GradingRankId",
                table: "KpiResults",
                column: "GradingRankId",
                principalTable: "GradingRanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KpiCheckIns_AppUsers_ReviewedByUserId",
                table: "KpiCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiCheckIns_AppUsers_SubmittedByUserId",
                table: "KpiCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiCheckIns_KpiFailReasons_KpiFailReasonId",
                table: "KpiCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiDefinitions_AppUsers_AssignerUserId",
                table: "KpiDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiDefinitions_EvaluationPeriods_EvaluationPeriodId",
                table: "KpiDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiDefinitions_OkrKeyResults_OkrKeyResultId",
                table: "KpiDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiDefinitions_OkrObjectives_OkrObjectiveId",
                table: "KpiDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiResults_EvaluationPeriods_EvaluationPeriodId",
                table: "KpiResults");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiResults_GradingRanks_GradingRankId",
                table: "KpiResults");

            migrationBuilder.DropTable(
                name: "AiGenerationHistories");

            migrationBuilder.DropTable(
                name: "BonusRules");

            migrationBuilder.DropTable(
                name: "EvaluationResults");

            migrationBuilder.DropTable(
                name: "KpiAdjustmentHistories");

            migrationBuilder.DropTable(
                name: "KpiCheckInDetails");

            migrationBuilder.DropTable(
                name: "KpiCheckInHistoryLogs");

            migrationBuilder.DropTable(
                name: "KpiDepartmentAssignments");

            migrationBuilder.DropTable(
                name: "KpiEmployeeAssignments");

            migrationBuilder.DropTable(
                name: "KpiFailReasons");

            migrationBuilder.DropTable(
                name: "KpiGoalComments");

            migrationBuilder.DropTable(
                name: "KpiResultComparisons");

            migrationBuilder.DropTable(
                name: "OkrDepartmentAllocations");

            migrationBuilder.DropTable(
                name: "OkrEmployeeAllocations");

            migrationBuilder.DropTable(
                name: "OkrKeyResults");

            migrationBuilder.DropTable(
                name: "OkrMissionMappings");

            migrationBuilder.DropTable(
                name: "OneOnOneMeetings");

            migrationBuilder.DropTable(
                name: "RealtimeExpectedBonuses");

            migrationBuilder.DropTable(
                name: "GradingRanks");

            migrationBuilder.DropTable(
                name: "MissionVisions");

            migrationBuilder.DropTable(
                name: "OkrObjectives");

            migrationBuilder.DropTable(
                name: "EvaluationPeriods");

            migrationBuilder.DropIndex(
                name: "IX_KpiResults_EvaluationPeriodId",
                table: "KpiResults");

            migrationBuilder.DropIndex(
                name: "IX_KpiResults_GradingRankId",
                table: "KpiResults");

            migrationBuilder.DropIndex(
                name: "IX_KpiDefinitions_AssignerUserId",
                table: "KpiDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_KpiDefinitions_EvaluationPeriodId",
                table: "KpiDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_KpiDefinitions_OkrKeyResultId",
                table: "KpiDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_KpiDefinitions_OkrObjectiveId",
                table: "KpiDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_KpiDefinitions_TenantId_Code",
                table: "KpiDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_KpiCheckIns_KpiFailReasonId",
                table: "KpiCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_KpiCheckIns_ReviewedByUserId",
                table: "KpiCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_KpiCheckIns_SubmittedByUserId",
                table: "KpiCheckIns");

            migrationBuilder.DropColumn(
                name: "CheckInFrequencyDays",
                table: "KpiTargets");

            migrationBuilder.DropColumn(
                name: "DeadlineTime",
                table: "KpiTargets");

            migrationBuilder.DropColumn(
                name: "FailThreshold",
                table: "KpiTargets");

            migrationBuilder.DropColumn(
                name: "PassThreshold",
                table: "KpiTargets");

            migrationBuilder.DropColumn(
                name: "ReminderEnabled",
                table: "KpiTargets");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "KpiResults");

            migrationBuilder.DropColumn(
                name: "EvaluationPeriodId",
                table: "KpiResults");

            migrationBuilder.DropColumn(
                name: "GradingRankId",
                table: "KpiResults");

            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "KpiResults");

            migrationBuilder.DropColumn(
                name: "AssignerUserId",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "EvaluationPeriodId",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "MeasureType",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "OkrKeyResultId",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "OkrObjectiveId",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "PropertyType",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "DeadlineAt",
                table: "KpiCheckIns");

            migrationBuilder.DropColumn(
                name: "IsLate",
                table: "KpiCheckIns");

            migrationBuilder.DropColumn(
                name: "KpiFailReasonId",
                table: "KpiCheckIns");

            migrationBuilder.DropColumn(
                name: "ReviewComment",
                table: "KpiCheckIns");

            migrationBuilder.DropColumn(
                name: "ReviewScore",
                table: "KpiCheckIns");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "KpiCheckIns");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "KpiCheckIns");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "KpiCheckIns");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "KpiCheckIns");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "KpiDefinitions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "KpiDefinitions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.CreateIndex(
                name: "IX_KpiDefinitions_TenantId",
                table: "KpiDefinitions",
                column: "TenantId");
        }
    }
}
