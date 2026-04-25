using Microsoft.EntityFrameworkCore;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Data;

public static class DemoIdentitySeeder
{
    public const string DemoPassword = "123";

    public static readonly DemoAccountSeed[] Accounts =
    [
        new("Admin", "Quản trị hệ thống", "admin@omnibiz.ai", "Admin Demo", 100, "Toàn quyền cấu hình hệ thống"),
        new("Director", "Giám đốc", "director@omnibiz.ai", "Director Demo", 80, "Theo dõi toàn công ty và phê duyệt cấp cao"),
        new("Manager", "Quản lý", "manager@omnibiz.ai", "Manager Demo", 60, "Quản lý phòng ban, KPI và đề nghị chi"),
        new("Accountant", "Kế toán", "accountant@omnibiz.ai", "Accountant Demo", 50, "Quản lý tài chính, ngân sách và giao dịch"),
        new("HR", "Nhân sự", "hr@omnibiz.ai", "HR Demo", 40, "Quản lý nhân sự, phòng ban và vị trí"),
        new("Staff", "Nhân viên", "staff@omnibiz.ai", "Staff Demo", 10, "Truy cập nghiệp vụ cá nhân")
    ];

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

        foreach (var account in Accounts)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == account.RoleName, cancellationToken);

            if (role is null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = account.RoleName,
                    DisplayName = account.RoleDisplayName,
                    Description = account.RoleDescription,
                    Level = account.RoleLevel,
                    IsSystem = true,
                    CreatedAt = now
                };
                db.Roles.Add(role);
            }
            else
            {
                role.DisplayName = account.RoleDisplayName;
                role.Description = account.RoleDescription;
                role.Level = account.RoleLevel;
                role.IsSystem = true;
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == account.Email, cancellationToken);

            if (user is null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = account.Email,
                    FullName = account.FullName,
                    CreatedAt = now
                };
                db.Users.Add(user);
            }

            user.PasswordHash = passwordHash;
            user.FullName = account.FullName;
            user.IsActive = true;
            user.IsLocked = false;
            user.LockedUntil = null;
            user.FailedLoginCount = 0;
            user.EmailConfirmed = true;
            user.IsDeleted = false;
            user.DeletedAt = null;
            user.UpdatedAt = now;

            await db.SaveChangesAsync(cancellationToken);

            var hasRole = await db.UserRoles.AnyAsync(
                ur => ur.UserId == user.Id && ur.RoleId == role.Id,
                cancellationToken);

            if (!hasRole)
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedAt = now
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record DemoAccountSeed(
    string RoleName,
    string RoleDisplayName,
    string Email,
    string FullName,
    int RoleLevel,
    string RoleDescription);
