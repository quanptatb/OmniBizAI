namespace OmniBizAI.Services;

public static class OperationRoles
{
    public const string Staff = "STAFF";
    public const string DepartmentManager = "DEPARTMENT_MANAGER";
    public const string TenantAdmin = "TENANT_ADMIN";
    public const string SystemAdmin = "SYSTEM_ADMIN";

    public const string CanCreate = $"{Staff},{DepartmentManager},{TenantAdmin},{SystemAdmin}";
    public const string CanContribute = CanCreate;
    public const string CanManageTemplates = $"{DepartmentManager},{TenantAdmin},{SystemAdmin}";
    public const string CanManageAssignments = CanManageTemplates;
    public const string CanDelete = $"{TenantAdmin},{SystemAdmin}";
}
