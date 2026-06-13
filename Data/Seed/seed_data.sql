-- Dọn dẹp dữ liệu cũ để tránh xung đột Foreign Key
UPDATE OrganizationUnits SET ParentId = NULL, ManagerUserId = NULL;
UPDATE AppUsers SET OrganizationUnitId = NULL;

DELETE FROM ProductTraceabilities;
DELETE FROM ProductionSteps;
DELETE FROM SalesOrderLines;
DELETE FROM SalesOrders;
DELETE FROM FailureModes;
DELETE FROM SparePartRequisitionLines;
DELETE FROM SparePartRequisitions;
DELETE FROM WorkOrderSparePartUsages;
DELETE FROM WorkOrderChecklistItems;
DELETE FROM WorkOrders;
DELETE FROM EquipmentSensorReadings;
DELETE FROM MaintenancePartUsages;
DELETE FROM MaintenanceRecords;
DELETE FROM MaintenanceIncidents;
DELETE FROM PmSchedules;
DELETE FROM SpareParts;
DELETE FROM ShiftAssignments;
DELETE FROM WorkShifts;
DELETE FROM EmployeeCertificates;
DELETE FROM Workspaces;
DELETE FROM PlanTaskDependencies;
DELETE FROM PlanTaskBaselines;
DELETE FROM PlanChangeOrders;
DELETE FROM PlanTasks;
DELETE FROM OperationPlans;
DELETE FROM EquipmentStatusHistories;
DELETE FROM EquipmentCostLedgers;
DELETE FROM Equipments;
DELETE FROM ReportDefinitions;
DELETE FROM DashboardWidgets;
DELETE FROM NotificationDeliveries;
DELETE FROM Notifications;
DELETE FROM ImportStagingRows;
DELETE FROM ImportJobs;
DELETE FROM AuditLogs;
DELETE FROM AiGenerationHistories;
DELETE FROM AiInsights;
DELETE FROM AiPromptTemplates;
DELETE FROM AiProviderConfigurations;
DELETE FROM OneOnOneMeetings;
DELETE FROM RealtimeExpectedBonuses;
DELETE FROM BonusRules;
DELETE FROM GradingRanks;
DELETE FROM EvaluationResults;
DELETE FROM EvaluationPeriods;
DELETE FROM OkrEmployeeAllocations;
DELETE FROM OkrDepartmentAllocations;
DELETE FROM OkrMissionMappings;
DELETE FROM OkrKeyResults;
DELETE FROM OkrObjectives;
DELETE FROM MissionVisions;
DELETE FROM KpiResultComparisons;
DELETE FROM KpiAdjustmentHistories;
DELETE FROM KpiEmployeeAssignments;
DELETE FROM KpiDepartmentAssignments;
DELETE FROM KpiFailReasons;
DELETE FROM KpiGoalComments;
DELETE FROM KpiCheckInHistoryLogs;
DELETE FROM KpiCheckInDetails;
DELETE FROM KpiCheckIns;
DELETE FROM KpiResults;
DELETE FROM KpiTargets;
DELETE FROM KpiDefinitions;
DELETE FROM Expenses;
DELETE FROM Budgets;
DELETE FROM PaymentRequestLines;
DELETE FROM PaymentRequests;
DELETE FROM CashTransactions;
DELETE FROM StockAlerts;
DELETE FROM GoodsIssueLines;
DELETE FROM GoodsIssues;
DELETE FROM GoodsReceiptLines;
DELETE FROM GoodsReceipts;
DELETE FROM PurchaseOrderLines;
DELETE FROM PurchaseOrders;
DELETE FROM ProcurementRequestLines;
DELETE FROM ProcurementRequests;
DELETE FROM ApprovalTasks;
DELETE FROM WorkflowHistories;
DELETE FROM WorkflowInstances;
DELETE FROM WorkflowTransitions;
DELETE FROM WorkflowSteps;
DELETE FROM WorkflowDefinitions;
DELETE FROM EntityTags;
DELETE FROM Tags;
DELETE FROM Attachments;
DELETE FROM KanbanSavedViews;
DELETE FROM Sprints;
DELETE FROM WorkItemDependencies;
DELETE FROM WorkItemComments;
DELETE FROM WorkItemChecklists;
DELETE FROM WorkItemActivities;
DELETE FROM WorkItemAssignments;
DELETE FROM WorkItems;
DELETE FROM KanbanColumns;
DELETE FROM OperationSlaBreaches;
DELETE FROM OperationSlaPolicies;
DELETE FROM OperationComments;
DELETE FROM OperationProgressLogs;
DELETE FROM OperationRequestAssignments;
DELETE FROM OperationRequestLines;
DELETE FROM OperationRequestTemplates;
DELETE FROM OperationRequests;
DELETE FROM UnitsOfMeasure;
DELETE FROM ProductServices;
DELETE FROM ProductCategories;
DELETE FROM Vendors;
DELETE FROM SalesOpportunities;
DELETE FROM CrmInteractions;
DELETE FROM CustomerSites;
DELETE FROM CustomerContacts;
DELETE FROM Customers;
DELETE FROM WorkCalendars;
DELETE FROM LeaveRequests;
DELETE FROM EmployeeContracts;
DELETE FROM EmployeeDepartmentAssignments;
DELETE FROM EmployeeProfiles;
DELETE FROM Positions;
DELETE FROM OrganizationUnits;
DELETE FROM BusinessProfiles;
DELETE FROM UserProfiles;
DELETE FROM UserTenants;
DELETE FROM UserSessions;
DELETE FROM UserRoleAssignments;
DELETE FROM PermissionAssignments;
DELETE FROM AppUsers;
DELETE FROM RoleDefinitions;
DELETE FROM PermissionDefinitions;
DELETE FROM TenantModules;
DELETE FROM TenantSettings;
DELETE FROM SystemParameters;
DELETE FROM NumberSequences;
DELETE FROM Tenants;

DELETE FROM AspNetUserRoles;
DELETE FROM AspNetUsers;
DELETE FROM AspNetRoles;

-- ============================================================================
-- OmniBizAI – Basic Seed Data
-- ============================================================================
SET NOCOUNT ON;

DECLARE @Now DATETIMEOFFSET = SYSDATETIMEOFFSET();
DECLARE @T UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';

-- ── Tenant ──
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE Id = @T)
INSERT INTO Tenants (Id, Code, [Name], BusinessType, [Status], CreatedAt, IsDeleted)
VALUES (@T, 'OMNIBIZ', N'Công ty TNHH Giải Pháp Số OmniBiz', N'Technology Services', 1, @Now, 0);

-- ── Organization Units ──
DECLARE @Root   UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000010';
DECLARE @BOD    UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000011';
DECLARE @IT     UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000012';
DECLARE @FIN    UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000013';
DECLARE @HR     UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000014';
DECLARE @SALE   UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000015';
DECLARE @MKT    UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000016';
DECLARE @OPS    UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000017';
DECLARE @QA     UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000018';
DECLARE @LEGAL  UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000019';
DECLARE @ADMIN  UNIQUEIDENTIFIER = '00000000-0000-0000-0000-00000000001A';
DECLARE @RND    UNIQUEIDENTIFIER = '00000000-0000-0000-0000-00000000001B';

IF NOT EXISTS (SELECT 1 FROM OrganizationUnits WHERE Id = @Root)
INSERT INTO OrganizationUnits (Id, TenantId, Code, [Name], [Level], ParentId, ManagerUserId, IsActive, CreatedAt, IsDeleted) VALUES
  (@Root,  @T, 'ROOT',  N'Công ty TNHH Giải Pháp Số OmniBiz', 0, NULL,  NULL, 1, @Now, 0),
  (@BOD,   @T, 'BOD',   N'Ban Giám Đốc',                      1, @Root, NULL, 1, @Now, 0),
  (@IT,    @T, 'IT',    N'Phòng Công Nghệ Thông Tin',          1, @Root, NULL, 1, @Now, 0),
  (@FIN,   @T, 'FIN',   N'Phòng Tài Chính – Kế Toán',         1, @Root, NULL, 1, @Now, 0),
  (@HR,    @T, 'HR',    N'Phòng Nhân Sự',                      1, @Root, NULL, 1, @Now, 0),
  (@SALE,  @T, 'SALE',  N'Phòng Kinh Doanh',                   1, @Root, NULL, 1, @Now, 0),
  (@MKT,   @T, 'MKT',   N'Phòng Marketing',                    1, @Root, NULL, 1, @Now, 0),
  (@OPS,   @T, 'OPS',   N'Phòng Vận Hành',                     1, @Root, NULL, 1, @Now, 0),
  (@QA,    @T, 'QA',    N'Phòng QA / Kiểm Thử',               1, @Root, NULL, 1, @Now, 0),
  (@LEGAL, @T, 'LEGAL', N'Phòng Pháp Chế',                     1, @Root, NULL, 1, @Now, 0),
  (@ADMIN, @T, 'ADM',   N'Phòng Hành Chính',                   1, @Root, NULL, 1, @Now, 0),
  (@RND,   @T, 'RND',   N'Phòng R&D / Nghiên Cứu',            1, @Root, NULL, 1, @Now, 0);

-- ── Identity Roles ──
DECLARE @RoleSysAdmin  UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000001';
DECLARE @RoleTenAdmin  UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000002';
DECLARE @RoleExec      UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000003';
DECLARE @RoleDeptMgr   UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000004';
DECLARE @RoleStaff     UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000005';
DECLARE @RoleAcct      UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000006';
DECLARE @RoleAuditor   UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000007';

IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Id = @RoleSysAdmin)
INSERT INTO AspNetRoles (Id, [Name], NormalizedName, ConcurrencyStamp) VALUES
  (@RoleSysAdmin, 'SYSTEM_ADMIN',       'SYSTEM_ADMIN',       NEWID()),
  (@RoleTenAdmin, 'TENANT_ADMIN',       'TENANT_ADMIN',       NEWID()),
  (@RoleExec,     'EXECUTIVE',          'EXECUTIVE',          NEWID()),
  (@RoleDeptMgr,  'DEPARTMENT_MANAGER', 'DEPARTMENT_MANAGER', NEWID()),
  (@RoleStaff,    'STAFF',              'STAFF',              NEWID()),
  (@RoleAcct,     'ACCOUNTANT',         'ACCOUNTANT',         NEWID()),
  (@RoleAuditor,  'AUDITOR',            'AUDITOR',            NEWID());

-- ── Role Definitions ──
IF NOT EXISTS (SELECT 1 FROM RoleDefinitions WHERE TenantId = @T AND Code = 'SYSTEM_ADMIN')
INSERT INTO RoleDefinitions (Id, TenantId, Code, [Name], [Description], IsSystemRole, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, 'SYSTEM_ADMIN',       N'Quản trị hệ thống',     NULL, 1, @Now, 0),
  (NEWID(), @T, 'TENANT_ADMIN',       N'Quản trị doanh nghiệp', NULL, 1, @Now, 0),
  (NEWID(), @T, 'EXECUTIVE',          N'Ban lãnh đạo',          NULL, 1, @Now, 0),
  (NEWID(), @T, 'DEPARTMENT_MANAGER', N'Trưởng bộ phận',        NULL, 1, @Now, 0),
  (NEWID(), @T, 'STAFF',              N'Nhân viên',             NULL, 0, @Now, 0),
  (NEWID(), @T, 'ACCOUNTANT',         N'Kế toán',               NULL, 0, @Now, 0),
  (NEWID(), @T, 'AUDITOR',            N'Kiểm soát',             NULL, 0, @Now, 0);

-- ── Positions ──
DECLARE @PosGD    UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000001';
DECLARE @PosPGD   UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000002';
DECLARE @PosTP    UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000003';
DECLARE @PosPTP   UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000004';
DECLARE @PosNV    UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000005';

IF NOT EXISTS (SELECT 1 FROM Positions WHERE Id = @PosGD)
INSERT INTO Positions (Id, TenantId, OrganizationUnitId, Code, [Name], [Level], IsManagerial, CreatedAt, IsDeleted) VALUES
  (@PosGD,  @T, @BOD,  'GD',  N'Giám Đốc',          1, 1, @Now, 0),
  (@PosPGD, @T, @BOD,  'PGD', N'Phó Giám Đốc',      2, 1, @Now, 0),
  (@PosTP,  @T, NULL,  'TP',  N'Trưởng Phòng',       3, 1, @Now, 0),
  (@PosPTP, @T, NULL,  'PTP', N'Phó Trưởng Phòng',   4, 1, @Now, 0),
  (@PosNV,  @T, NULL,  'NV',  N'Nhân Viên',          5, 0, @Now, 0);

-- ── Core Users (9 accounts) ──
DECLARE @U01 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001'; -- Giám đốc
DECLARE @U04 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004'; -- TP IT
DECLARE @U05 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000005'; -- TP Finance
DECLARE @U06 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000006'; -- TP HR
DECLARE @U07 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000007'; -- TP Sales
DECLARE @U09 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000009'; -- TP Ops
DECLARE @U14 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-00000000000E'; -- System Admin
DECLARE @U15 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-00000000000F'; -- Kế toán trưởng
DECLARE @U21 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000021'; -- Nhân viên (vu.ngoc.hai)

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @U01)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount) VALUES
  (@U01, 'giamdoc@omnibiz.vn',       'GIAMDOC@OMNIBIZ.VN',       'giamdoc@omnibiz.vn',       'GIAMDOC@OMNIBIZ.VN',       1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U04, 'tp.it@omnibiz.vn',         'TP.IT@OMNIBIZ.VN',         'tp.it@omnibiz.vn',         'TP.IT@OMNIBIZ.VN',         1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U05, 'tp.finance@omnibiz.vn',    'TP.FINANCE@OMNIBIZ.VN',    'tp.finance@omnibiz.vn',    'TP.FINANCE@OMNIBIZ.VN',    1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U06, 'tp.hr@omnibiz.vn',         'TP.HR@OMNIBIZ.VN',         'tp.hr@omnibiz.vn',         'TP.HR@OMNIBIZ.VN',         1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U07, 'tp.sales@omnibiz.vn',      'TP.SALES@OMNIBIZ.VN',      'tp.sales@omnibiz.vn',      'TP.SALES@OMNIBIZ.VN',      1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U09, 'tp.ops@omnibiz.vn',        'TP.OPS@OMNIBIZ.VN',        'tp.ops@omnibiz.vn',        'TP.OPS@OMNIBIZ.VN',        1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U14, 'sysadmin@omnibiz.vn',      'SYSADMIN@OMNIBIZ.VN',      'sysadmin@omnibiz.vn',      'SYSADMIN@OMNIBIZ.VN',      1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U15, 'ketoan.truong@omnibiz.vn', 'KETOAN.TRUONG@OMNIBIZ.VN', 'ketoan.truong@omnibiz.vn', 'KETOAN.TRUONG@OMNIBIZ.VN', 1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U21, 'vu.ngoc.hai@omnibiz.vn',    'VU.NGOC.HAI@OMNIBIZ.VN',    'vu.ngoc.hai@omnibiz.vn',    'VU.NGOC.HAI@OMNIBIZ.VN',    1,NULL,NEWID(),NEWID(),0,0,1,0);

IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @U14 AND RoleId = @RoleSysAdmin)
INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES
  (@U14, @RoleSysAdmin), (@U14, @RoleTenAdmin),
  (@U01, @RoleExec),
  (@U04, @RoleDeptMgr), (@U05, @RoleDeptMgr), (@U06, @RoleDeptMgr),
  (@U07, @RoleDeptMgr), (@U09, @RoleDeptMgr), (@U15, @RoleAcct),
  (@U21, @RoleStaff);

IF NOT EXISTS (SELECT 1 FROM AppUsers WHERE Id = @U01)
INSERT INTO AppUsers (Id, TenantId, FullName, Email, JobTitle, OrganizationUnitId, [Status], CreatedAt, IsDeleted) VALUES
  (@U01, @T, N'Nguyễn Minh Tuấn',    'giamdoc@omnibiz.vn',       N'Giám Đốc',              @BOD,   1, @Now, 0),
  (@U04, @T, N'Phạm Đức Anh',        'tp.it@omnibiz.vn',         N'Trưởng Phòng IT',        @IT,    1, @Now, 0),
  (@U05, @T, N'Võ Thị Lan Anh',      'tp.finance@omnibiz.vn',    N'Trưởng Phòng Tài Chính', @FIN,   1, @Now, 0),
  (@U06, @T, N'Đặng Văn Khôi',       'tp.hr@omnibiz.vn',         N'Trưởng Phòng Nhân Sự',   @HR,    1, @Now, 0),
  (@U07, @T, N'Hoàng Thị Mai',       'tp.sales@omnibiz.vn',      N'Trưởng Phòng Kinh Doanh',@SALE,  1, @Now, 0),
  (@U09, @T, N'Ngô Thị Thanh Hằng',  'tp.ops@omnibiz.vn',        N'Trưởng Phòng Vận Hành',  @OPS,   1, @Now, 0),
  (@U14, @T, N'System Administrator', 'sysadmin@omnibiz.vn',      N'System Admin',           @IT,    1, @Now, 0),
  (@U15, @T, N'Nguyễn Thị Hạnh',     'ketoan.truong@omnibiz.vn', N'Kế Toán Trưởng',         @FIN,   1, @Now, 0),
  (@U21, @T, N'Vũ Ngọc Hải',         'vu.ngoc.hai@omnibiz.vn',    N'Nhân Viên Kinh Doanh',   @SALE,  1, @Now, 0);

-- Update OrgUnit managers
UPDATE OrganizationUnits SET ManagerUserId = @U01 WHERE Id = @Root;
UPDATE OrganizationUnits SET ManagerUserId = @U01 WHERE Id = @BOD;
UPDATE OrganizationUnits SET ManagerUserId = @U04 WHERE Id = @IT;
UPDATE OrganizationUnits SET ManagerUserId = @U05 WHERE Id = @FIN;
UPDATE OrganizationUnits SET ManagerUserId = @U06 WHERE Id = @HR;
UPDATE OrganizationUnits SET ManagerUserId = @U07 WHERE Id = @SALE;
UPDATE OrganizationUnits SET ManagerUserId = @U09 WHERE Id = @OPS;

-- ── Business Profile ──
IF NOT EXISTS (SELECT 1 FROM BusinessProfiles WHERE TenantId = @T AND Code = 'MAIN')
INSERT INTO BusinessProfiles (Id, TenantId, Code, [Name], Industry, ConfigurationJson, IsDefault, CreatedAt, IsDeleted)
VALUES (NEWID(), @T, 'MAIN', N'OmniBiz Digital Solutions', N'Technology Services',
  '{"TaxCode":"0315678901","Address":"Tầng 12, Tòa nhà Landmark Plus, Q. Bình Thạnh, TP.HCM","Phone":"028-3820-9999","Website":"https://omnibiz.vn","FoundedYear":2015,"EmployeeCount":105}',
  1, @Now, 0);

-- ── Employee Profiles ──
IF NOT EXISTS (SELECT 1 FROM EmployeeProfiles WHERE TenantId = @T)
INSERT INTO EmployeeProfiles (Id, TenantId, UserId, EmployeeCode, StartDate, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @U01, 'EMP-0001', CAST(@Now AS DATE), @Now, 0),
  (NEWID(), @T, @U04, 'EMP-0004', CAST(@Now AS DATE), @Now, 0),
  (NEWID(), @T, @U05, 'EMP-0005', CAST(@Now AS DATE), @Now, 0),
  (NEWID(), @T, @U06, 'EMP-0006', CAST(@Now AS DATE), @Now, 0),
  (NEWID(), @T, @U07, 'EMP-0007', CAST(@Now AS DATE), @Now, 0),
  (NEWID(), @T, @U09, 'EMP-0009', CAST(@Now AS DATE), @Now, 0),
  (NEWID(), @T, @U14, 'EMP-0014', CAST(@Now AS DATE), @Now, 0),
  (NEWID(), @T, @U15, 'EMP-0015', CAST(@Now AS DATE), @Now, 0),
  (NEWID(), @T, @U21, 'EMP-0021', CAST(@Now AS DATE), @Now, 0);

-- ── System Parameters ──
IF NOT EXISTS (SELECT 1 FROM SystemParameters WHERE [Key] = 'SYS_LOCALE')
INSERT INTO SystemParameters (Id, TenantId, [Key], [Value], [Group], ValueType, IsEditable, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, 'SYS_LOCALE', 'vi-VN', N'System', 'String', 0, @Now, 0),
  (NEWID(), @T, 'CURRENCY', 'VND', N'System', 'String', 1, @Now, 0);

PRINT N'✅ Basic Seed Data hoàn tất.';
