using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniBizAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationsStatusEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_TenantId_Status_DueDate",
                table: "WorkItems");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Workspaces",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WorkItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "WorkItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ShiftAssignments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Frequency",
                table: "PmSchedules",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PlanTasks",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.DropIndex(
                name: "IX_OperationRequests_Dashboard_Status_DueDate",
                table: "OperationRequests");

            migrationBuilder.DropIndex(
                name: "IX_OperationRequests_Dashboard_Status_UpdatedAt",
                table: "OperationRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "OperationRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequests_Dashboard_Status_DueDate",
                table: "OperationRequests",
                columns: new[] { "TenantId", "IsDeleted", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequests_Dashboard_Status_UpdatedAt",
                table: "OperationRequests",
                columns: new[] { "TenantId", "IsDeleted", "Status", "UpdatedAt" });

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "OperationRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "OperationPlans",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MaintenanceRecords",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "MaintenanceType",
                table: "MaintenanceRecords",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MaintenanceIncidents",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "MaintenanceIncidents",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "EquipmentSensorReadings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Equipments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.DropIndex(
                name: "IX_ApprovalTasks_Dashboard_Status",
                table: "ApprovalTasks");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ApprovalTasks",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalTasks_Dashboard_Status",
                table: "ApprovalTasks",
                columns: new[] { "TenantId", "IsDeleted", "Status" });

            migrationBuilder.Sql(@"
UPDATE [OperationRequests]
SET [Status] = CASE [Status]
    WHEN N'1' THEN N'Draft'
    WHEN N'2' THEN N'Submitted'
    WHEN N'3' THEN N'InReview'
    WHEN N'4' THEN N'Approved'
    WHEN N'5' THEN N'InProgress'
    WHEN N'6' THEN N'Completed'
    WHEN N'7' THEN N'Rejected'
    WHEN N'8' THEN N'Cancelled'
    WHEN N'9' THEN N'OnHold'
    ELSE [Status]
END;

UPDATE [OperationRequests]
SET [Priority] = CASE [Priority]
    WHEN N'1' THEN N'Low'
    WHEN N'2' THEN N'Normal'
    WHEN N'3' THEN N'High'
    WHEN N'4' THEN N'Critical'
    ELSE [Priority]
END;

UPDATE [WorkItems]
SET [Status] = CASE [Status]
    WHEN N'1' THEN N'Todo'
    WHEN N'2' THEN N'InProgress'
    WHEN N'3' THEN N'Blocked'
    WHEN N'4' THEN N'Done'
    WHEN N'5' THEN N'Cancelled'
    ELSE [Status]
END;

UPDATE [WorkItems]
SET [Priority] = CASE [Priority]
    WHEN N'1' THEN N'Low'
    WHEN N'2' THEN N'Normal'
    WHEN N'3' THEN N'High'
    WHEN N'4' THEN N'Critical'
    ELSE [Priority]
END;

UPDATE [ApprovalTasks]
SET [Status] = CASE [Status]
    WHEN N'1' THEN N'Pending'
    WHEN N'2' THEN N'Approved'
    WHEN N'3' THEN N'Rejected'
    WHEN N'4' THEN N'Skipped'
    WHEN N'5' THEN N'Cancelled'
    ELSE [Status]
END;

UPDATE [PmSchedules]
SET [Frequency] = CASE [Frequency]
    WHEN N'Every_X_Hours' THEN N'ByRunHours'
    WHEN N'Every_X_Cycles' THEN N'ByCycles'
    ELSE [Frequency]
END;
");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_TenantId_Status_DueDate",
                table: "WorkItems",
                columns: new[] { "TenantId", "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_TenantId_Status_DueDate",
                table: "WorkItems");

            migrationBuilder.Sql(@"
UPDATE [OperationRequests]
SET [Status] = CASE [Status]
    WHEN N'Draft' THEN N'1'
    WHEN N'Submitted' THEN N'2'
    WHEN N'InReview' THEN N'3'
    WHEN N'Approved' THEN N'4'
    WHEN N'InProgress' THEN N'5'
    WHEN N'Completed' THEN N'6'
    WHEN N'Rejected' THEN N'7'
    WHEN N'Cancelled' THEN N'8'
    WHEN N'OnHold' THEN N'9'
    ELSE [Status]
END;

UPDATE [OperationRequests]
SET [Priority] = CASE [Priority]
    WHEN N'Low' THEN N'1'
    WHEN N'Normal' THEN N'2'
    WHEN N'High' THEN N'3'
    WHEN N'Critical' THEN N'4'
    ELSE [Priority]
END;

UPDATE [WorkItems]
SET [Status] = CASE [Status]
    WHEN N'Todo' THEN N'1'
    WHEN N'InProgress' THEN N'2'
    WHEN N'Blocked' THEN N'3'
    WHEN N'Done' THEN N'4'
    WHEN N'Cancelled' THEN N'5'
    ELSE [Status]
END;

UPDATE [WorkItems]
SET [Priority] = CASE [Priority]
    WHEN N'Low' THEN N'1'
    WHEN N'Normal' THEN N'2'
    WHEN N'High' THEN N'3'
    WHEN N'Critical' THEN N'4'
    ELSE [Priority]
END;

UPDATE [ApprovalTasks]
SET [Status] = CASE [Status]
    WHEN N'Pending' THEN N'1'
    WHEN N'Approved' THEN N'2'
    WHEN N'Rejected' THEN N'3'
    WHEN N'Skipped' THEN N'4'
    WHEN N'Cancelled' THEN N'5'
    ELSE [Status]
END;

UPDATE [PmSchedules]
SET [Frequency] = CASE [Frequency]
    WHEN N'ByRunHours' THEN N'Every_X_Hours'
    WHEN N'ByCycles' THEN N'Every_X_Cycles'
    ELSE [Frequency]
END;
");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Workspaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "WorkItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "WorkItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ShiftAssignments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Frequency",
                table: "PmSchedules",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PlanTasks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.DropIndex(
                name: "IX_OperationRequests_Dashboard_Status_DueDate",
                table: "OperationRequests");

            migrationBuilder.DropIndex(
                name: "IX_OperationRequests_Dashboard_Status_UpdatedAt",
                table: "OperationRequests");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "OperationRequests",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequests_Dashboard_Status_DueDate",
                table: "OperationRequests",
                columns: new[] { "TenantId", "IsDeleted", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationRequests_Dashboard_Status_UpdatedAt",
                table: "OperationRequests",
                columns: new[] { "TenantId", "IsDeleted", "Status", "UpdatedAt" });

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "OperationRequests",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "OperationPlans",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MaintenanceRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "MaintenanceType",
                table: "MaintenanceRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MaintenanceIncidents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "MaintenanceIncidents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "EquipmentSensorReadings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Equipments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.DropIndex(
                name: "IX_ApprovalTasks_Dashboard_Status",
                table: "ApprovalTasks");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "ApprovalTasks",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalTasks_Dashboard_Status",
                table: "ApprovalTasks",
                columns: new[] { "TenantId", "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_TenantId_Status_DueDate",
                table: "WorkItems",
                columns: new[] { "TenantId", "Status", "DueDate" });
        }
    }
}
