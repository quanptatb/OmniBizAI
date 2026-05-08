-- ============================================================================
-- OmniBizAI – Seed Data
-- Chạy file này trong SSMS sau khi app đã tạo bảng (EF Core migrations).
-- Idempotent: chạy lại nhiều lần không bị lỗi.
-- Password cho Identity users được set bởi app (Program.cs) vì SQL 
-- không thể tạo ASP.NET Identity password hash.
-- ============================================================================

-- ── Fixed GUIDs ──────────────────────────────────────────────────────────────
DECLARE @TenantId   UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @RootOrgId  UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000010';
DECLARE @ItDeptId   UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000011';
DECLARE @FinDeptId  UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000012';
DECLARE @HrDeptId   UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000013';
DECLARE @AdminId    UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000030';
DECLARE @ExecId     UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000031';
DECLARE @ManagerId  UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000032';
DECLARE @StaffId    UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000033';
DECLARE @AccountId  UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000034';

DECLARE @RoleSysAdmin  UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000001';
DECLARE @RoleTenAdmin  UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000002';
DECLARE @RoleExec      UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000003';
DECLARE @RoleDeptMgr   UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000004';
DECLARE @RoleStaff     UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000005';
DECLARE @RoleAcct      UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000006';
DECLARE @RoleAuditor   UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000007';

DECLARE @Now DATETIMEOFFSET = SYSDATETIMEOFFSET();

-- ============================================================================
-- 1) Identity Roles
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Id = @RoleSysAdmin)
INSERT INTO AspNetRoles (Id, [Name], NormalizedName, ConcurrencyStamp) VALUES
  (@RoleSysAdmin, 'SYSTEM_ADMIN',       'SYSTEM_ADMIN',       NEWID()),
  (@RoleTenAdmin, 'TENANT_ADMIN',       'TENANT_ADMIN',       NEWID()),
  (@RoleExec,     'EXECUTIVE',          'EXECUTIVE',          NEWID()),
  (@RoleDeptMgr,  'DEPARTMENT_MANAGER', 'DEPARTMENT_MANAGER', NEWID()),
  (@RoleStaff,    'STAFF',              'STAFF',              NEWID()),
  (@RoleAcct,     'ACCOUNTANT',         'ACCOUNTANT',         NEWID()),
  (@RoleAuditor,  'AUDITOR',            'AUDITOR',            NEWID());

-- ============================================================================
-- 2) Identity Users (password = 123, hash sẽ được app set tự động)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @AdminId)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount) VALUES
  (@AdminId,   'admin@omnibiz.demo',     'ADMIN@OMNIBIZ.DEMO',     'admin@omnibiz.demo',     'ADMIN@OMNIBIZ.DEMO',     1, NULL, NEWID(), NEWID(), 0, 0, 1, 0),
  (@ExecId,    'giamdoc@omnibiz.demo',   'GIAMDOC@OMNIBIZ.DEMO',   'giamdoc@omnibiz.demo',   'GIAMDOC@OMNIBIZ.DEMO',   1, NULL, NEWID(), NEWID(), 0, 0, 1, 0),
  (@ManagerId, 'manager.it@omnibiz.demo','MANAGER.IT@OMNIBIZ.DEMO','manager.it@omnibiz.demo','MANAGER.IT@OMNIBIZ.DEMO', 1, NULL, NEWID(), NEWID(), 0, 0, 1, 0),
  (@StaffId,   'staff@omnibiz.demo',     'STAFF@OMNIBIZ.DEMO',     'staff@omnibiz.demo',     'STAFF@OMNIBIZ.DEMO',     1, NULL, NEWID(), NEWID(), 0, 0, 1, 0),
  (@AccountId, 'accountant@omnibiz.demo','ACCOUNTANT@OMNIBIZ.DEMO','accountant@omnibiz.demo','ACCOUNTANT@OMNIBIZ.DEMO', 1, NULL, NEWID(), NEWID(), 0, 0, 1, 0);

-- ============================================================================
-- 3) Identity UserRoles
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @AdminId AND RoleId = @RoleSysAdmin)
INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES
  (@AdminId,   @RoleSysAdmin),
  (@AdminId,   @RoleTenAdmin),
  (@ExecId,    @RoleExec),
  (@ManagerId, @RoleDeptMgr),
  (@StaffId,   @RoleStaff),
  (@AccountId, @RoleAcct);

-- ============================================================================
-- 4) Tenant
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE Id = @TenantId)
INSERT INTO Tenants (Id, Code, [Name], BusinessType, [Status], CreatedAt, IsDeleted)
VALUES (@TenantId, 'OMNIBIZ-DEMO', N'OmniBiz Demo Company', N'Technology Services', 1, @Now, 0);

-- ============================================================================
-- 5) Organization Units
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM OrganizationUnits WHERE Id = @RootOrgId)
INSERT INTO OrganizationUnits (Id, TenantId, Code, [Name], [Level], ParentId, ManagerUserId, IsActive, CreatedAt, IsDeleted) VALUES
  (@RootOrgId, @TenantId, 'ROOT', N'OmniBiz Demo Company',       0, NULL,       NULL,       1, @Now, 0),
  (@ItDeptId,  @TenantId, 'IT',   N'Phòng Công Nghệ Thông Tin',  1, @RootOrgId, @ManagerId, 1, @Now, 0),
  (@FinDeptId, @TenantId, 'FIN',  N'Phòng Tài Chính - Kế Toán',  1, @RootOrgId, NULL,       1, @Now, 0),
  (@HrDeptId,  @TenantId, 'HR',   N'Phòng Nhân Sự',              1, @RootOrgId, NULL,       1, @Now, 0);

-- ============================================================================
-- 6) AppUsers
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM AppUsers WHERE Id = @AdminId)
INSERT INTO AppUsers (Id, TenantId, FullName, Email, JobTitle, OrganizationUnitId, [Status], CreatedAt, IsDeleted) VALUES
  (@AdminId,   @TenantId, N'System Administrator',  'admin@omnibiz.demo',     N'System Admin',        @RootOrgId, 1, @Now, 0),
  (@ExecId,    @TenantId, N'Nguyễn Văn Giám Đốc',  'giamdoc@omnibiz.demo',   N'Giám Đốc Điều Hành', @RootOrgId, 1, @Now, 0),
  (@ManagerId, @TenantId, N'Trần Thị Quản Lý IT',  'manager.it@omnibiz.demo',N'Trưởng Phòng IT',     @ItDeptId,  1, @Now, 0),
  (@StaffId,   @TenantId, N'Lê Văn Nhân Viên',     'staff@omnibiz.demo',     N'Nhân Viên IT',        @ItDeptId,  1, @Now, 0),
  (@AccountId, @TenantId, N'Phạm Thị Kế Toán',    'accountant@omnibiz.demo',N'Kế Toán Trưởng',      @FinDeptId, 1, @Now, 0);

-- ============================================================================
-- 7) Role Definitions
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM RoleDefinitions WHERE TenantId = @TenantId AND Code = 'SYSTEM_ADMIN')
INSERT INTO RoleDefinitions (Id, TenantId, Code, [Name], [Description], IsSystemRole, CreatedAt, IsDeleted) VALUES
  (NEWID(), @TenantId, 'SYSTEM_ADMIN',       N'Quản trị hệ thống',     NULL, 1, @Now, 0),
  (NEWID(), @TenantId, 'TENANT_ADMIN',       N'Quản trị doanh nghiệp', NULL, 1, @Now, 0),
  (NEWID(), @TenantId, 'EXECUTIVE',          N'Ban lãnh đạo',          NULL, 1, @Now, 0),
  (NEWID(), @TenantId, 'DEPARTMENT_MANAGER', N'Trưởng bộ phận',        NULL, 1, @Now, 0),
  (NEWID(), @TenantId, 'STAFF',              N'Nhân viên',             NULL, 0, @Now, 0),
  (NEWID(), @TenantId, 'ACCOUNTANT',         N'Kế toán',               NULL, 0, @Now, 0),
  (NEWID(), @TenantId, 'AUDITOR',            N'Kiểm soát',             NULL, 0, @Now, 0);

-- ============================================================================
-- 8) Operation Requests
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM OperationRequests WHERE TenantId = @TenantId AND RequestNo = 'OPR-2026-001')
INSERT INTO OperationRequests (Id, TenantId, RequestNo, [Type], Title, OrganizationUnitId, RequestedByUserId, [Priority], [Status], DueDate, TotalAmount, [Description], CreatedAt, IsDeleted) VALUES
  (NEWID(), @TenantId, 'OPR-2026-001', 'InternalRequest', N'Yêu cầu nâng cấp máy chủ phát triển',  @ItDeptId, @StaffId,   3, 3, DATEADD(DAY,14,GETDATE()), 25000000, N'Nâng cấp RAM và CPU cho máy chủ dev team',  DATEADD(DAY,-5,@Now),  0),
  (NEWID(), @TenantId, 'OPR-2026-002', 'ServiceRequest',  N'Yêu cầu tổ chức training nội bộ',       @HrDeptId, @StaffId,   2, 1, DATEADD(DAY,30,GETDATE()), 5000000,  N'Training kỹ năng mềm Q2/2026',             DATEADD(DAY,-2,@Now),  0),
  (NEWID(), @TenantId, 'OPR-2026-003', 'InternalRequest', N'Triển khai hệ thống monitoring',         @ItDeptId, @ManagerId, 4, 4, DATEADD(DAY,7,GETDATE()),  15000000, N'Setup Grafana + Prometheus cho production', DATEADD(DAY,-10,@Now), 0),
  (NEWID(), @TenantId, 'OPR-2026-004', 'InternalRequest', N'Mua license phần mềm thiết kế',          @ItDeptId, @StaffId,   2, 6, DATEADD(DAY,-3,GETDATE()), 8000000,  N'License Adobe Creative Suite 2026',         DATEADD(DAY,-20,@Now), 0),
  (NEWID(), @TenantId, 'OPR-2026-005', 'ServiceRequest',  N'Yêu cầu sửa chữa văn phòng',            @HrDeptId, @StaffId,   1, 7, DATEADD(DAY,-7,GETDATE()), 3000000,  N'Sơn lại tường khu vực lễ tân',              DATEADD(DAY,-15,@Now), 0);

-- ============================================================================
-- 9) Budgets
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM Budgets WHERE TenantId = @TenantId AND Code = 'BUD-IT-Q2')
INSERT INTO Budgets (Id, TenantId, OrganizationUnitId, Code, [Name], FiscalYear, PlannedAmount, [Status], CreatedAt, IsDeleted) VALUES
  (NEWID(), @TenantId, @ItDeptId,  'BUD-IT-Q2',  N'Ngân sách Q2 2026 - IT',       2026, 200000000, 2, @Now, 0),
  (NEWID(), @TenantId, @FinDeptId, 'BUD-FIN-Q2', N'Ngân sách Q2 2026 - Tài chính', 2026, 100000000, 2, @Now, 0),
  (NEWID(), @TenantId, @HrDeptId,  'BUD-HR-Q2',  N'Ngân sách Q2 2026 - Nhân sự',   2026, 80000000,  2, @Now, 0);

-- ============================================================================
-- 10) KPI Definitions
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM KpiDefinitions WHERE TenantId = @TenantId AND Code = 'KPI-OP-01')
INSERT INTO KpiDefinitions (Id, TenantId, OrganizationUnitId, Code, [Name], Unit, OwnerType, PeriodType, IsActive, CreatedAt, IsDeleted) VALUES
  (NEWID(), @TenantId, @ItDeptId,  'KPI-OP-01',   N'Tỷ lệ hoàn thành yêu cầu đúng hạn', N'%',   2, 1, 1, @Now, 0),
  (NEWID(), @TenantId, NULL,       'KPI-SAT-01',  N'Độ hài lòng khách hàng',              N'/10', 1, 2, 1, @Now, 0),
  (NEWID(), @TenantId, @ItDeptId,  'KPI-REQ-01',  N'Số yêu cầu xử lý mỗi tháng',         N'YC',  2, 1, 1, @Now, 0),
  (NEWID(), @TenantId, @FinDeptId, 'KPI-COST-01', N'Chi phí vận hành',                     N'VND', 2, 1, 1, @Now, 0);

-- ============================================================================
-- 11) Customers
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM Customers WHERE TenantId = @TenantId AND Code = 'KH-001')
INSERT INTO Customers (Id, TenantId, Code, [Name], TaxCode, Industry, IsActive, CreatedAt, IsDeleted) VALUES
  (NEWID(), @TenantId, 'KH-001', N'Công ty CP Đầu Tư ABC', '0312345678', N'Tài chính',    1, @Now, 0),
  (NEWID(), @TenantId, 'KH-002', N'Tập đoàn XYZ Holdings', '0398765432', N'Bất động sản', 1, @Now, 0),
  (NEWID(), @TenantId, 'KH-003', N'CTCP Thương Mại DEF',   '0312399999', N'Thương mại',   1, @Now, 0);

-- ============================================================================
-- 12) Notifications
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE TenantId = @TenantId AND Title = N'Hệ thống OmniBizAI đã sẵn sàng')
INSERT INTO Notifications (Id, TenantId, Title, Body, [Status], PublishedAt, CreatedAt, IsDeleted) VALUES
  (NEWID(), @TenantId, N'Yêu cầu mới cần duyệt',           N'OPR-2026-001 đang chờ phê duyệt',                    2, DATEADD(HOUR,-2,@Now), DATEADD(HOUR,-2,@Now), 0),
  (NEWID(), @TenantId, N'KPI tháng 4 sắp đến hạn báo cáo', N'Vui lòng cập nhật kết quả KPI trước ngày 05/05/2026', 2, DATEADD(DAY,-1,@Now),  DATEADD(DAY,-1,@Now),  0),
  (NEWID(), @TenantId, N'Hệ thống OmniBizAI đã sẵn sàng',  N'Chào mừng đến với OmniBizAI',                         2, DATEADD(DAY,-7,@Now),  DATEADD(DAY,-7,@Now),  0);

-- ============================================================================
-- 13) AI Prompt Template
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM AiPromptTemplates WHERE TenantId = @TenantId AND Code = 'DASHBOARD_ANALYSIS')
INSERT INTO AiPromptTemplates (Id, TenantId, Code, ContextType, [Version], SystemPrompt, UserPromptTemplate, IsActive, CreatedAt, IsDeleted) VALUES
  (NEWID(), @TenantId, 'DASHBOARD_ANALYSIS', 'Dashboard', 1,
   N'Bạn là trợ lý phân tích vận hành cho doanh nghiệp vừa và nhỏ.',
   N'CONTEXT: {{business_context}} QUESTION: {{user_question}}',
   1, @Now, 0);

PRINT N'✅ Seed data OK. Chạy lại app để set password cho Identity users.';
