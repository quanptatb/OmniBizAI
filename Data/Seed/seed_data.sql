-- Dọn dẹp dữ liệu cũ để tránh xung đột Foreign Key
DELETE FROM AuditLogs;
DELETE FROM AiInsights;
DELETE FROM Attachments;
DELETE FROM WorkItemComments;
DELETE FROM EvaluationResults;
DELETE FROM EvaluationPeriods;
DELETE FROM KpiCheckIns;
DELETE FROM ApprovalTasks;
DELETE FROM Expenses;
DELETE FROM PaymentRequests;
DELETE FROM Budgets;
DELETE FROM Customers;
DELETE FROM WorkItemChecklists;
DELETE FROM WorkItemAssignments;
DELETE FROM WorkItems;
DELETE FROM OperationRequests;
DELETE FROM PurchaseOrders;
DELETE FROM Vendors;
DELETE FROM KpiTargets;
DELETE FROM KpiDefinitions;
DELETE FROM OkrKeyResults;
DELETE FROM OkrObjectives;
DELETE FROM AppUsers;
DELETE FROM Positions;
DELETE FROM OrganizationUnits;
DELETE FROM BusinessProfiles;
DELETE FROM RoleDefinitions;
DELETE FROM Tenants;

DELETE FROM AspNetUserRoles;
DELETE FROM AspNetUsers;
DELETE FROM AspNetRoles;

-- ============================================================================
-- OmniBizAI – Master Seed Data
-- ============================================================================
SET NOCOUNT ON;

DECLARE @Now DATETIMEOFFSET = SYSDATETIMEOFFSET();
DECLARE @CompanyStart DATETIMEOFFSET = '2015-03-15T08:00:00+07:00';

-- ── Tenant ──
DECLARE @T UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM Tenants WHERE Id = @T)
INSERT INTO Tenants (Id, Code, [Name], BusinessType, [Status], CreatedAt, IsDeleted)
VALUES (@T, 'OMNIBIZ', N'Công ty TNHH Giải Pháp Số OmniBiz', N'Technology Services', 1, @CompanyStart, 0);

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
  (@Root,  @T, 'ROOT',  N'Công ty TNHH Giải Pháp Số OmniBiz', 0, NULL,  NULL, 1, @CompanyStart, 0),
  (@BOD,   @T, 'BOD',   N'Ban Giám Đốc',                      1, @Root, NULL, 1, @CompanyStart, 0),
  (@IT,    @T, 'IT',    N'Phòng Công Nghệ Thông Tin',          1, @Root, NULL, 1, @CompanyStart, 0),
  (@FIN,   @T, 'FIN',   N'Phòng Tài Chính – Kế Toán',         1, @Root, NULL, 1, @CompanyStart, 0),
  (@HR,    @T, 'HR',    N'Phòng Nhân Sự',                      1, @Root, NULL, 1, @CompanyStart, 0),
  (@SALE,  @T, 'SALE',  N'Phòng Kinh Doanh',                   1, @Root, NULL, 1, @CompanyStart, 0),
  (@MKT,   @T, 'MKT',  N'Phòng Marketing',                    1, @Root, NULL, 1, @CompanyStart, 0),
  (@OPS,   @T, 'OPS',   N'Phòng Vận Hành',                     1, @Root, NULL, 1, @CompanyStart, 0),
  (@QA,    @T, 'QA',    N'Phòng QA / Kiểm Thử',               1, @Root, NULL, 1, @CompanyStart, 0),
  (@LEGAL, @T, 'LEGAL', N'Phòng Pháp Chế',                     1, @Root, NULL, 1, @CompanyStart, 0),
  (@ADMIN, @T, 'ADM',   N'Phòng Hành Chính',                   1, @Root, NULL, 1, @CompanyStart, 0),
  (@RND,   @T, 'RND',   N'Phòng R&D / Nghiên Cứu',            1, @Root, NULL, 1, @CompanyStart, 0);

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
  (NEWID(), @T, 'SYSTEM_ADMIN',       N'Quản trị hệ thống',     NULL, 1, @CompanyStart, 0),
  (NEWID(), @T, 'TENANT_ADMIN',       N'Quản trị doanh nghiệp', NULL, 1, @CompanyStart, 0),
  (NEWID(), @T, 'EXECUTIVE',          N'Ban lãnh đạo',          NULL, 1, @CompanyStart, 0),
  (NEWID(), @T, 'DEPARTMENT_MANAGER', N'Trưởng bộ phận',        NULL, 1, @CompanyStart, 0),
  (NEWID(), @T, 'STAFF',              N'Nhân viên',             NULL, 0, @CompanyStart, 0),
  (NEWID(), @T, 'ACCOUNTANT',         N'Kế toán',               NULL, 0, @CompanyStart, 0),
  (NEWID(), @T, 'AUDITOR',            N'Kiểm soát',             NULL, 0, @CompanyStart, 0);

-- ── Positions ──
DECLARE @PosGD    UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000001';
DECLARE @PosPGD   UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000002';
DECLARE @PosTP    UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000003';
DECLARE @PosPTP   UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000004';
DECLARE @PosNV    UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000005';
DECLARE @PosTL    UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000006';
DECLARE @PosIntern UNIQUEIDENTIFIER = 'C0000000-0000-0000-0000-000000000007';

IF NOT EXISTS (SELECT 1 FROM Positions WHERE Id = @PosGD)
INSERT INTO Positions (Id, TenantId, OrganizationUnitId, Code, [Name], [Level], IsManagerial, CreatedAt, IsDeleted) VALUES
  (@PosGD,     @T, @BOD,  'GD',     N'Giám Đốc',          1, 1, @CompanyStart, 0),
  (@PosPGD,    @T, @BOD,  'PGD',    N'Phó Giám Đốc',      2, 1, @CompanyStart, 0),
  (@PosTP,     @T, NULL,  'TP',     N'Trưởng Phòng',       3, 1, @CompanyStart, 0),
  (@PosPTP,    @T, NULL,  'PTP',    N'Phó Trưởng Phòng',   4, 1, @CompanyStart, 0),
  (@PosTL,     @T, NULL,  'TL',     N'Team Leader',        5, 1, @CompanyStart, 0),
  (@PosNV,     @T, NULL,  'NV',     N'Nhân Viên',          6, 0, @CompanyStart, 0),
  (@PosIntern, @T, NULL,  'INTERN', N'Thực Tập Sinh',      7, 0, @CompanyStart, 0);

-- ============================================================================
-- KEY USERS (Ban lãnh đạo + Trưởng phòng) – 15 người
-- ============================================================================
DECLARE @U01 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001'; -- Giám đốc
DECLARE @U02 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002'; -- Phó GĐ Kinh doanh
DECLARE @U03 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000003'; -- Phó GĐ Kỹ thuật
DECLARE @U04 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004'; -- TP IT
DECLARE @U05 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000005'; -- TP Finance
DECLARE @U06 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000006'; -- TP HR
DECLARE @U07 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000007'; -- TP Sales
DECLARE @U08 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000008'; -- TP Marketing
DECLARE @U09 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000009'; -- TP Ops
DECLARE @U10 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-00000000000A'; -- TP QA
DECLARE @U11 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-00000000000B'; -- TP Legal
DECLARE @U12 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-00000000000C'; -- TP Admin
DECLARE @U13 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-00000000000D'; -- TP R&D
DECLARE @U14 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-00000000000E'; -- System Admin
DECLARE @U15 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-00000000000F'; -- Kế toán trưởng

-- Ghi chú: Mật khẩu mặc định là '123'. 
-- PasswordHash được để NULL ở đây và sẽ được tự động băm & cập nhật bởi Program.cs lúc khởi động.
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @U01)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount) VALUES
  (@U01, 'giamdoc@omnibiz.vn',       'GIAMDOC@OMNIBIZ.VN',       'giamdoc@omnibiz.vn',       'GIAMDOC@OMNIBIZ.VN',       1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U02, 'pgd.kinhdoanh@omnibiz.vn', 'PGD.KINHDOANH@OMNIBIZ.VN', 'pgd.kinhdoanh@omnibiz.vn', 'PGD.KINHDOANH@OMNIBIZ.VN', 1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U03, 'pgd.kythuat@omnibiz.vn',   'PGD.KYTHUAT@OMNIBIZ.VN',   'pgd.kythuat@omnibiz.vn',   'PGD.KYTHUAT@OMNIBIZ.VN',   1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U04, 'tp.it@omnibiz.vn',         'TP.IT@OMNIBIZ.VN',         'tp.it@omnibiz.vn',         'TP.IT@OMNIBIZ.VN',         1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U05, 'tp.finance@omnibiz.vn',    'TP.FINANCE@OMNIBIZ.VN',    'tp.finance@omnibiz.vn',    'TP.FINANCE@OMNIBIZ.VN',    1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U06, 'tp.hr@omnibiz.vn',         'TP.HR@OMNIBIZ.VN',         'tp.hr@omnibiz.vn',         'TP.HR@OMNIBIZ.VN',         1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U07, 'tp.sales@omnibiz.vn',      'TP.SALES@OMNIBIZ.VN',      'tp.sales@omnibiz.vn',      'TP.SALES@OMNIBIZ.VN',      1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U08, 'tp.marketing@omnibiz.vn',  'TP.MARKETING@OMNIBIZ.VN',  'tp.marketing@omnibiz.vn',  'TP.MARKETING@OMNIBIZ.VN',  1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U09, 'tp.ops@omnibiz.vn',        'TP.OPS@OMNIBIZ.VN',        'tp.ops@omnibiz.vn',        'TP.OPS@OMNIBIZ.VN',        1,NULL,NEWID(),NEWID(),0,0,1,0),

  (@U10, 'tp.qa@omnibiz.vn',         'TP.QA@OMNIBIZ.VN',         'tp.qa@omnibiz.vn',         'TP.QA@OMNIBIZ.VN',         1,NULL,NEWID(),NEWID(),0,0,1,0),  (@U11, 'tp.legal@omnibiz.vn',      'TP.LEGAL@OMNIBIZ.VN',      'tp.legal@omnibiz.vn',      'TP.LEGAL@OMNIBIZ.VN',      1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U12, 'tp.admin@omnibiz.vn',      'TP.ADMIN@OMNIBIZ.VN',      'tp.admin@omnibiz.vn',      'TP.ADMIN@OMNIBIZ.VN',      1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U13, 'tp.rnd@omnibiz.vn',        'TP.RND@OMNIBIZ.VN',        'tp.rnd@omnibiz.vn',        'TP.RND@OMNIBIZ.VN',        1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U14, 'sysadmin@omnibiz.vn',      'SYSADMIN@OMNIBIZ.VN',      'sysadmin@omnibiz.vn',      'SYSADMIN@OMNIBIZ.VN',      1,NULL,NEWID(),NEWID(),0,0,1,0),
  (@U15, 'ketoan.truong@omnibiz.vn', 'KETOAN.TRUONG@OMNIBIZ.VN', 'ketoan.truong@omnibiz.vn', 'KETOAN.TRUONG@OMNIBIZ.VN', 1,NULL,NEWID(),NEWID(),0,0,1,0);

IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @U14 AND RoleId = @RoleSysAdmin)
INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES
  (@U14, @RoleSysAdmin), (@U14, @RoleTenAdmin),
  (@U01, @RoleExec), (@U02, @RoleExec), (@U03, @RoleExec),
  (@U04, @RoleDeptMgr), (@U05, @RoleDeptMgr), (@U06, @RoleDeptMgr),
  (@U07, @RoleDeptMgr), (@U08, @RoleDeptMgr), (@U09, @RoleDeptMgr),
  (@U10, @RoleDeptMgr), (@U11, @RoleDeptMgr), (@U12, @RoleDeptMgr),
  (@U13, @RoleDeptMgr), (@U15, @RoleAcct);

IF NOT EXISTS (SELECT 1 FROM AppUsers WHERE Id = @U01)
INSERT INTO AppUsers (Id, TenantId, FullName, Email, JobTitle, OrganizationUnitId, [Status], CreatedAt, IsDeleted) VALUES
  (@U01, @T, N'Nguyễn Minh Tuấn',    'giamdoc@omnibiz.vn',       N'Giám Đốc',              @BOD,   1, @CompanyStart, 0),
  (@U02, @T, N'Trần Thị Hồng Nhung', 'pgd.kinhdoanh@omnibiz.vn', N'Phó GĐ Kinh Doanh',     @BOD,   1, @CompanyStart, 0),
  (@U03, @T, N'Lê Văn Hùng',         'pgd.kythuat@omnibiz.vn',   N'Phó GĐ Kỹ Thuật',       @BOD,   1, @CompanyStart, 0),
  (@U04, @T, N'Phạm Đức Anh',        'tp.it@omnibiz.vn',         N'Trưởng Phòng IT',        @IT,    1, @CompanyStart, 0),
  (@U05, @T, N'Võ Thị Lan Anh',      'tp.finance@omnibiz.vn',    N'Trưởng Phòng Tài Chính', @FIN,   1, @CompanyStart, 0),
  (@U06, @T, N'Đặng Văn Khôi',       'tp.hr@omnibiz.vn',         N'Trưởng Phòng Nhân Sự',   @HR,    1, @CompanyStart, 0),
  (@U07, @T, N'Hoàng Thị Mai',       'tp.sales@omnibiz.vn',      N'Trưởng Phòng Kinh Doanh',@SALE,  1, @CompanyStart, 0),
  (@U08, @T, N'Bùi Quang Hải',       'tp.marketing@omnibiz.vn',  N'Trưởng Phòng Marketing', @MKT,   1, @CompanyStart, 0),
  (@U09, @T, N'Ngô Thị Thanh Hằng',  'tp.ops@omnibiz.vn',        N'Trưởng Phòng Vận Hành',  @OPS,   1, @CompanyStart, 0),
  (@U10, @T, N'Đinh Công Thành',     'tp.qa@omnibiz.vn',         N'Trưởng Phòng QA',        @QA,    1, @CompanyStart, 0),
  (@U11, @T, N'Lý Thị Ngọc Bích',   'tp.legal@omnibiz.vn',      N'Trưởng Phòng Pháp Chế',  @LEGAL, 1, @CompanyStart, 0),
  (@U12, @T, N'Trương Văn Đạt',      'tp.admin@omnibiz.vn',      N'Trưởng Phòng Hành Chính',@ADMIN, 1, @CompanyStart, 0),
  (@U13, @T, N'Phan Minh Quân',      'tp.rnd@omnibiz.vn',        N'Trưởng Phòng R&D',       @RND,   1, @CompanyStart, 0),
  (@U14, @T, N'System Administrator', 'sysadmin@omnibiz.vn',      N'System Admin',           @IT,    1, @CompanyStart, 0),
  (@U15, @T, N'Nguyễn Thị Hạnh',     'ketoan.truong@omnibiz.vn', N'Kế Toán Trưởng',         @FIN,   1, @CompanyStart, 0);

-- Update OrgUnit managers
UPDATE OrganizationUnits SET ManagerUserId = @U01 WHERE Id = @Root;
UPDATE OrganizationUnits SET ManagerUserId = @U01 WHERE Id = @BOD;
UPDATE OrganizationUnits SET ManagerUserId = @U04 WHERE Id = @IT;
UPDATE OrganizationUnits SET ManagerUserId = @U05 WHERE Id = @FIN;
UPDATE OrganizationUnits SET ManagerUserId = @U06 WHERE Id = @HR;
UPDATE OrganizationUnits SET ManagerUserId = @U07 WHERE Id = @SALE;
UPDATE OrganizationUnits SET ManagerUserId = @U08 WHERE Id = @MKT;
UPDATE OrganizationUnits SET ManagerUserId = @U09 WHERE Id = @OPS;
UPDATE OrganizationUnits SET ManagerUserId = @U10 WHERE Id = @QA;
UPDATE OrganizationUnits SET ManagerUserId = @U11 WHERE Id = @LEGAL;
UPDATE OrganizationUnits SET ManagerUserId = @U12 WHERE Id = @ADMIN;
UPDATE OrganizationUnits SET ManagerUserId = @U13 WHERE Id = @RND;

-- ── Business Profile ──
IF NOT EXISTS (SELECT 1 FROM BusinessProfiles WHERE TenantId = @T AND Code = 'MAIN')
INSERT INTO BusinessProfiles (Id, TenantId, Code, [Name], Industry, ConfigurationJson, IsDefault, CreatedAt, IsDeleted)
VALUES (NEWID(), @T, 'MAIN', N'OmniBiz Digital Solutions', N'Technology Services',
  '{"TaxCode":"0315678901","Address":"Tầng 12, Tòa nhà Landmark Plus, Q. Bình Thạnh, TP.HCM","Phone":"028-3820-9999","Website":"https://omnibiz.vn","FoundedYear":2015,"EmployeeCount":105}',
  1, @CompanyStart, 0);

PRINT N'✅ Part 1: Core setup hoàn tất.';

-- ============================================================================
-- STAFF EMPLOYEES
-- ============================================================================
CREATE TABLE #StaffData (
    Id UNIQUEIDENTIFIER,
    FullName NVARCHAR(200),
    Email NVARCHAR(255),
    JobTitle NVARCHAR(150),
    OrgUnitId UNIQUEIDENTIFIER,
    RoleId UNIQUEIDENTIFIER
);

INSERT INTO #StaffData VALUES 
(NEWID(), N'Lê Thị Hương', 'lethi.huong@omnibiz.vn', N'Kế toán viên', @FIN, @RoleAcct),
(NEWID(), N'Trần Văn Tâm', 'tranvan.tam@omnibiz.vn', N'Kế toán viên', @FIN, @RoleAcct),
(NEWID(), N'Nguyễn Thanh Tùng', 'nguyen.thanh.tung@omnibiz.vn', N'Chuyên viên IT Helpdesk', @IT, @RoleStaff),
(NEWID(), N'Phạm Quang Dũng', 'pham.quang.dung@omnibiz.vn', N'System Engineer', @IT, @RoleStaff),
(NEWID(), N'Đỗ Thị Lệ', 'do.thi.le@omnibiz.vn', N'Chuyên viên Tuyển dụng', @HR, @RoleStaff),
(NEWID(), N'Bùi Thị Thu Thủy', 'bui.thi.thu.thuy@omnibiz.vn', N'Chuyên viên C&B', @HR, @RoleStaff),
(NEWID(), N'Vũ Ngọc Hải', 'vu.ngoc.hai@omnibiz.vn', N'Nhân viên Kinh doanh', @SALE, @RoleStaff),
(NEWID(), N'Hoàng Anh Tuấn', 'hoang.anh.tuan@omnibiz.vn', N'Nhân viên Kinh doanh', @SALE, @RoleStaff),
(NEWID(), N'Đặng Phương Nam', 'dang.phuong.nam@omnibiz.vn', N'Nhân viên Kinh doanh', @SALE, @RoleStaff),
(NEWID(), N'Lương Tú Mai', 'luong.tu.mai@omnibiz.vn', N'Nhân viên Kinh doanh', @SALE, @RoleStaff),
(NEWID(), N'Ngô Hữu Phước', 'ngo.huu.phuoc@omnibiz.vn', N'Chuyên viên Marketing', @MKT, @RoleStaff),
(NEWID(), N'Trịnh Yến Nhi', 'trinh.yen.nhi@omnibiz.vn', N'Content Creator', @MKT, @RoleStaff),
(NEWID(), N'Đoàn Văn Hậu', 'doan.van.hau@omnibiz.vn', N'Chuyên viên Vận hành', @OPS, @RoleStaff),
(NEWID(), N'Lâm Minh Nhật', 'lam.minh.nhat@omnibiz.vn', N'Chuyên viên Vận hành', @OPS, @RoleStaff),
(NEWID(), N'Tạ Quang Thắng', 'ta.quang.thang@omnibiz.vn', N'QA Tester', @QA, @RoleStaff),
(NEWID(), N'Vương Thị Kim Chi', 'vuong.thi.kim.chi@omnibiz.vn', N'QA Automation', @QA, @RoleStaff),
(NEWID(), N'Mai Văn Lực', 'mai.van.luc@omnibiz.vn', N'Chuyên viên Pháp lý', @LEGAL, @RoleStaff),
(NEWID(), N'Hồ Bảo Trâm', 'ho.bao.tram@omnibiz.vn', N'Nhân viên Hành chính', @ADMIN, @RoleStaff),
(NEWID(), N'Phùng Khắc Dũng', 'phung.khac.dung@omnibiz.vn', N'Bảo vệ', @ADMIN, @RoleStaff),
(NEWID(), N'Thái Văn Hùng', 'thai.van.hung@omnibiz.vn', N'Lái xe', @ADMIN, @RoleStaff),
(NEWID(), N'Khổng Tấn Đạt', 'khong.tan.dat@omnibiz.vn', N'R&D Engineer', @RND, @RoleStaff),
(NEWID(), N'Chu Hoàng Long', 'chu.hoang.long@omnibiz.vn', N'Data Scientist', @RND, @RoleStaff);

INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
SELECT Id, Email, UPPER(Email), Email, UPPER(Email), 1, NULL, NEWID(), NEWID(), 0, 0, 1, 0
FROM #StaffData
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers u WHERE u.Email = #StaffData.Email);

UPDATE t SET t.Id = u.Id FROM #StaffData t JOIN AspNetUsers u ON t.Email = u.Email;

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT Id, RoleId FROM #StaffData
WHERE NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur WHERE ur.UserId = #StaffData.Id AND ur.RoleId = #StaffData.RoleId);

INSERT INTO AppUsers (Id, TenantId, FullName, Email, JobTitle, OrganizationUnitId, [Status], CreatedAt, IsDeleted)
SELECT Id, @T, FullName, Email, JobTitle, OrgUnitId, 1, @CompanyStart, 0
FROM #StaffData
WHERE NOT EXISTS (SELECT 1 FROM AppUsers au WHERE au.Id = #StaffData.Id);

DROP TABLE #StaffData;

PRINT N'✅ Part 2: Staff employees setup hoàn tất.';

-- ============================================================================
-- BUSINESS OPERATIONS (OKRs, KPIs, WorkItems, POs)
-- ============================================================================

DECLARE @Okr1 UNIQUEIDENTIFIER = 'E0000000-0000-0000-0000-000000000001';
DECLARE @Okr2 UNIQUEIDENTIFIER = 'E0000000-0000-0000-0000-000000000002';

IF NOT EXISTS (SELECT 1 FROM OkrObjectives WHERE Id = @Okr1)
INSERT INTO OkrObjectives (Id, TenantId, ObjectiveName, [Level], Cycle, [Status], IsActive, CreatedAt, IsDeleted) VALUES
  (@Okr1, @T, N'Tăng trưởng doanh thu 30% trong năm nay',                0, '2026',    1, 1, @Now, 0),
  (@Okr2, @T, N'Cải thiện chất lượng phần mềm, giảm thiểu lỗi',         1, 'Q2-2026', 1, 1, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM OkrKeyResults WHERE OkrObjectiveId = @Okr1)
INSERT INTO OkrKeyResults (Id, TenantId, OkrObjectiveId, KeyResultName, Unit, TargetValue, CurrentValue, IsInverse, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @Okr1, N'Đạt mốc doanh thu 50 tỷ VNĐ',             N'Tỷ VNĐ',      50.0,   15.0, 0, @Now, 0),
  (NEWID(), @T, @Okr1, N'Có 20 khách hàng doanh nghiệp mới',        N'Khách hàng',   20.0,    5.0, 0, @Now, 0),
  (NEWID(), @T, @Okr2, N'Tỷ lệ lỗi (bug rate) dưới 2%',             N'%',             2.0,    4.5, 1, @Now, 0),
  (NEWID(), @T, @Okr2, N'Thời gian uptime hệ thống 99.99%',          N'%',            99.99,  99.9, 0, @Now, 0);

DECLARE @Kpi1 UNIQUEIDENTIFIER = 'F0000000-0000-0000-0000-000000000001';
DECLARE @Kpi2 UNIQUEIDENTIFIER = 'F0000000-0000-0000-0000-000000000002';
DECLARE @KpiSaleDept UNIQUEIDENTIFIER = ISNULL(@SALE, @IT);

IF NOT EXISTS (SELECT 1 FROM KpiDefinitions WHERE Id = @Kpi1)
INSERT INTO KpiDefinitions (Id, TenantId, OrganizationUnitId, Code, [Name], [Description], Unit, OwnerType, PeriodType, MeasureType, PropertyType, [Status], IsActive, AssignerUserId, CreatedAt, IsDeleted) VALUES
  (@Kpi1, @T, @KpiSaleDept, 'KPI-SALE-001', N'Doanh số bán hàng hàng tháng',        N'Chỉ tiêu doanh số mang về mỗi tháng cho từng khu vực',        N'Triệu VNĐ', 1, 2, 0, 0, 1, 1, @U01, @Now, 0),
  (@Kpi2, @T, @IT,          'KPI-IT-001',   N'Tỷ lệ phản hồi ticket < 15 phút',     N'SLA xử lý sự cố nội bộ và khách hàng',                        N'%',          1, 2, 0, 0, 1, 1, @U01, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM KpiTargets WHERE KpiDefinitionId = @Kpi1)
INSERT INTO KpiTargets (Id, TenantId, KpiDefinitionId, OwnerUserId, OrganizationUnitId, PeriodStart, PeriodEnd, TargetValue, PassThreshold, FailThreshold, CheckInFrequencyDays, ReminderEnabled, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @Kpi1, @U07, @KpiSaleDept, '2026-05-01', '2026-05-31', 2000.0, 1800.0, 1500.0, 7, 1, @Now, 0),
  (NEWID(), @T, @Kpi2, @U04, @IT,          '2026-05-01', '2026-05-31',   95.0,   90.0,   80.0, 7, 1, @Now, 0);

DECLARE @OpReq1 UNIQUEIDENTIFIER = 'D0000000-0000-0000-0000-000000000001';

IF @IT IS NOT NULL AND NOT EXISTS (SELECT 1 FROM OperationRequests WHERE Id = @OpReq1)
INSERT INTO OperationRequests (Id, TenantId, RequestNo, [Type], Title, OrganizationUnitId, RequestedByUserId, Priority, [Status], DueDate, TotalAmount, [Description], CreatedAt, IsDeleted) VALUES
  (@OpReq1, @T, 'REQ-2026-0001', 'IT-SUPPORT', N'Nâng cấp hệ thống server core', @IT, @U04, 2, 1, '2026-06-15', 50000000.0,
   N'Cần nâng cấp RAM và ổ cứng cho cụm server database để đáp ứng tải tăng cao.', @Now, 0);

IF EXISTS (SELECT 1 FROM OperationRequests WHERE Id = @OpReq1)
   AND NOT EXISTS (SELECT 1 FROM WorkItems WHERE OperationRequestId = @OpReq1)
INSERT INTO WorkItems (Id, TenantId, OperationRequestId, OrganizationUnitId, Title, [Description], [Status], Priority, DueDate, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @OpReq1, @IT, N'Khảo sát và đánh giá hệ thống hiện tại',
   N'Cần kiểm tra log và usage của server hiện tại để xác định cấu hình phù hợp.', 1, 2, '2026-05-20', @Now, 0),
  (NEWID(), @T, @OpReq1, @IT, N'Lên kế hoạch mua sắm thiết bị',
   N'Làm việc với Vendor để xin báo giá linh kiện.', 0, 2, '2026-05-25', @Now, 0);

DECLARE @Vendor1 UNIQUEIDENTIFIER = 'A1000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM Vendors WHERE Id = @Vendor1)
INSERT INTO Vendors (Id, TenantId, Code, [Name], TaxCode, Email, PhoneNumber, IsActive, CreatedAt, IsDeleted) VALUES
  (@Vendor1, @T, 'VND-DELL', N'Công ty TNHH Dell Việt Nam', '0101234567', 'sales@dell.com.vn', '18001234', 1, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM PurchaseOrders WHERE VendorId = @Vendor1)
INSERT INTO PurchaseOrders (Id, TenantId, VendorId, OrderNo, [Status], OrderDate, TotalAmount, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @Vendor1, 'PO-2026-001', 1, '2026-05-15', 50000000.0, @Now, 0);

PRINT N'✅ Part 3: Business Operations setup hoàn tất.';

-- ============================================================================
-- 4. NEW SEED DATA (CRM, Finance, Evaluation, Approval, AI, Audit)
-- ============================================================================

-- 4.1. Customers (CRM)
DECLARE @Cust1 UNIQUEIDENTIFIER = 'B0000000-0000-0000-0000-000000000001';
DECLARE @Cust2 UNIQUEIDENTIFIER = 'B0000000-0000-0000-0000-000000000002';

IF NOT EXISTS (SELECT 1 FROM Customers WHERE Id = @Cust1)
INSERT INTO Customers (Id, TenantId, Code, [Name], TaxCode, IsActive, CreatedAt, IsDeleted) VALUES
  (@Cust1, @T, 'CUST-001', N'Công ty Cổ phần Xây dựng Hòa Bình', '0301234567', 1, @Now, 0),
  (@Cust2, @T, 'CUST-002', N'Tập đoàn Vingroup', '0101245367', 1, @Now, 0);

-- 4.2. Finance (Budgets, Expenses, PaymentRequests)
DECLARE @BudgetIT UNIQUEIDENTIFIER = 'BD000000-0000-0000-0000-000000000001';
DECLARE @BudgetMkt UNIQUEIDENTIFIER = 'BD000000-0000-0000-0000-000000000002';

IF NOT EXISTS (SELECT 1 FROM Budgets WHERE Id = @BudgetIT)
INSERT INTO Budgets (Id, TenantId, Code, [Name], OrganizationUnitId, FiscalYear, PlannedAmount, [Status], CreatedAt, IsDeleted) VALUES
  (@BudgetIT, @T, 'BD-IT-Q2', N'Ngân sách IT Q2/2026', @IT, 2026, 200000000.0, 1, @Now, 0),
  (@BudgetMkt, @T, 'BD-MKT-M5', N'Ngân sách Marketing T5/2026', @MKT, 2026, 500000000.0, 1, @Now, 0);

DECLARE @PayReq1 UNIQUEIDENTIFIER = 'B3000000-0000-0000-0000-000000000001';
IF NOT EXISTS (SELECT 1 FROM PaymentRequests WHERE Id = @PayReq1)
INSERT INTO PaymentRequests (Id, TenantId, RequestNo, RequestedByUserId, TotalAmount, [Status], CreatedAt, IsDeleted) VALUES
  (@PayReq1, @T, 'PAY-2026-0001', @U04, 25000000.0, 1, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM Expenses WHERE BudgetId = @BudgetIT)
INSERT INTO Expenses (Id, TenantId, BudgetId, PaymentRequestId, Amount, [Description], ExpenseDate, [Status], CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @BudgetIT, @PayReq1, 25000000.0, N'Tạm ứng đợt 1 nâng cấp Server', @Now, 1, @Now, 0);

-- 4.3. Approvals
DECLARE @Appr1 UNIQUEIDENTIFIER = 'B4000000-0000-0000-0000-000000000001';
IF NOT EXISTS (SELECT 1 FROM ApprovalTasks WHERE Id = @Appr1)
INSERT INTO ApprovalTasks (Id, TenantId, TargetType, TargetId, StepCode, AssignedToUserId, AssignedRole, [Status], CreatedAt, IsDeleted) VALUES
  (@Appr1, @T, 'OperationRequest', @OpReq1, 'DEPARTMENT_REVIEW', NULL, 'DEPARTMENT_MANAGER', 1, DATEADD(day, -2, @Now), 0),
  (NEWID(), @T, 'OperationRequest', @OpReq1, 'EXECUTIVE_APPROVE', @U01, NULL, 0, @Now, 0),
  (NEWID(), @T, 'PaymentRequest', @PayReq1, 'FINANCE_REVIEW', @U05, NULL, 0, @Now, 0);

-- Update Approval Decision for the first one
UPDATE ApprovalTasks SET DecisionNote = N'Đồng ý triển khai gấp', DecidedAt = @Now WHERE Id = @Appr1;

-- 4.4. WorkItem Assignments & Checklists
DECLARE @WorkItem1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM WorkItems WHERE Title = N'Khảo sát và đánh giá hệ thống hiện tại');
DECLARE @WorkItem2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM WorkItems WHERE Title = N'Lên kế hoạch mua sắm thiết bị');

IF @WorkItem1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM WorkItemAssignments WHERE WorkItemId = @WorkItem1)
INSERT INTO WorkItemAssignments (Id, TenantId, WorkItemId, AssignedToUserId, AssignedAt, CreatedByUserId, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @WorkItem1, @U04, @Now, @U04, @Now, 0); -- Assigned to TP IT

IF @WorkItem1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM WorkItemChecklists WHERE WorkItemId = @WorkItem1)
INSERT INTO WorkItemChecklists (Id, TenantId, WorkItemId, Title, IsCompleted, SortOrder, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @WorkItem1, N'Kiểm tra RAM usage server web', 1, 1, @Now, 0),
  (NEWID(), @T, @WorkItem1, N'Kiểm tra Disk IOPS server db', 1, 2, @Now, 0),
  (NEWID(), @T, @WorkItem1, N'Tổng hợp report đánh giá', 0, 3, @Now, 0);

-- 4.5. KPI Check-ins
DECLARE @KpiTarget1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM KpiTargets WHERE OwnerUserId = @U07);
IF @KpiTarget1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM KpiCheckIns WHERE KpiTargetId = @KpiTarget1)
INSERT INTO KpiCheckIns (Id, TenantId, KpiTargetId, UserId, CheckInDate, ProgressValue, Comment, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @KpiTarget1, @U07, DATEADD(day, -7, @Now), 500.0, N'Tuần 1: Chốt được 3 hợp đồng', @Now, 0),
  (NEWID(), @T, @KpiTarget1, @U07, @Now, 1200.0, N'Tuần 2: Tiến độ tốt', @Now, 0);

-- 4.6. Evaluation (Kỳ đánh giá & Kết quả)
DECLARE @EvalPeriod UNIQUEIDENTIFIER = 'B5000000-0000-0000-0000-000000000001';
IF NOT EXISTS (SELECT 1 FROM EvaluationPeriods WHERE Id = @EvalPeriod)
INSERT INTO EvaluationPeriods (Id, TenantId, PeriodName, StartDate, EndDate, [Status], CreatedAt, IsDeleted) VALUES
  (@EvalPeriod, @T, N'Đánh giá hiệu suất Quý 1/2026', '2026-04-01', '2026-04-15', 2, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM EvaluationResults WHERE EvaluationPeriodId = @EvalPeriod)
INSERT INTO EvaluationResults (Id, TenantId, EvaluationPeriodId, UserId, TotalScore, Classification, ReviewComment, SubmissionStatus, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @EvalPeriod, @U07, 92.0, 'A', N'Hoàn thành xuất sắc chỉ tiêu doanh số', 2, @Now, 0),
  (NEWID(), @T, @EvalPeriod, @U04, 85.0, 'B+', N'Hệ thống ổn định, cần cải thiện thời gian fix bug', 2, @Now, 0);

-- 4.7. Comments & Attachments
IF @WorkItem1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM WorkItemComments WHERE WorkItemId = @WorkItem1)
INSERT INTO WorkItemComments (Id, TenantId, WorkItemId, UserId, Content, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @WorkItem1, @U01, N'Gấp rút hoàn thành khảo sát trong tuần này nhé!', DATEADD(hour, -2, @Now), 0),
  (NEWID(), @T, @WorkItem1, @U04, N'Đã hoàn thành 2/3 checklist sếp ạ.', DATEADD(hour, -1, @Now), 0);

-- 4.8. AI Insights
IF NOT EXISTS (SELECT 1 FROM AiInsights WHERE TenantId = @T)
INSERT INTO AiInsights (Id, TenantId, ContextType, ContextId, Question, Summary, Recommendation, RiskLevel, [Status], AskedByUserId, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, 'OperationRequest', @OpReq1, N'Phân tích rủi ro của việc nâng cấp server', N'Việc nâng cấp server db có thể gây downtime 2-4 tiếng ảnh hưởng kinh doanh.', N'Thực hiện nâng cấp vào 2h sáng Chủ Nhật. Cần chuẩn bị phương án backup dữ liệu.', 1, 1, @U04, DATEADD(hour, -5, @Now), 0);

-- 4.9. Audit Logs
IF NOT EXISTS (SELECT 1 FROM AuditLogs WHERE TenantId = @T)
INSERT INTO AuditLogs (Id, TenantId, UserId, UserName, [Action], EntityName, EntityId, OldValuesJson, NewValuesJson, CreatedAt, IsDeleted) VALUES
  (NEWID(), @T, @U01, N'Nguyễn Minh Tuấn', 'Login', 'System', NULL, NULL, '{"IpAddress": "192.168.1.5"}', DATEADD(minute, -30, @Now), 0),
  (NEWID(), @T, @U04, N'Phạm Đức Anh', 'Update', 'WorkItem', @WorkItem1, '{"Status": "Todo"}', '{"Status": "InProgress"}', DATEADD(minute, -15, @Now), 0);

PRINT N'✅ Part 4: CRM, Finance, Approvals, Evaluation, AI & Audit setup hoàn tất.';
PRINT N'🎉 Seed Data Master Script hoàn tất.';
