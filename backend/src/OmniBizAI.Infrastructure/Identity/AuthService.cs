using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OmniBizAI.Application.Common;
using OmniBizAI.Application.DTOs;
using OmniBizAI.Application.Interfaces;
using OmniBizAI.Domain.Entities.Identity;
using OmniBizAI.Domain.Entities.Organization;
using OmniBizAI.Domain.Interfaces;
using OmniBizAI.Infrastructure.Data;

namespace OmniBizAI.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private const int AccessTokenMinutes = 60;
    private const int RefreshTokenDays = 7;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, ApplicationDbContext dbContext)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = _unitOfWork.Repository<User>().Query().FirstOrDefault(x => x.Email == email)
            ?? throw new BusinessRuleException("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
        {
            throw new BusinessRuleException("Account is inactive.");
        }

        if (user.IsLocked && user.LockedUntil > DateTime.UtcNow)
        {
            throw new BusinessRuleException($"Account is locked until {user.LockedUntil:O}.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount += 1;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.IsLocked = true;
                user.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
            }

            await RecordLoginAttemptAsync(email, ipAddress, false, "Invalid credentials", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleException("Email hoặc mật khẩu không đúng.");
        }

        user.FailedLoginCount = 0;
        user.IsLocked = false;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        await RecordLoginAttemptAsync(email, ipAddress, true, null, cancellationToken);
        await _unitOfWork.Repository<UserSession>().AddAsync(new UserSession
        {
            UserId = user.Id,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken);

        var refreshToken = CreateRefreshToken(user.Id, ipAddress);
        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BuildLoginResponse(user, refreshToken.Token);
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var token = _unitOfWork.Repository<RefreshToken>().Query().FirstOrDefault(x => x.Token == request.RefreshToken)
            ?? throw new BusinessRuleException("Invalid refresh token.");
        if (token.RevokedAt is not null || token.ExpiresAt <= DateTime.UtcNow)
        {
            throw new BusinessRuleException("Refresh token has expired or been revoked.");
        }

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(token.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var replacement = CreateRefreshToken(user.Id, ipAddress);
        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByToken = replacement.Token;
        await _unitOfWork.Repository<RefreshToken>().AddAsync(replacement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return BuildLoginResponse(user, replacement.Token);
    }

    private LoginResponse BuildLoginResponse(User user, string refreshToken)
    {
        var roles = _unitOfWork.Repository<UserRole>().Query()
            .Where(x => x.UserId == user.Id)
            .Join(_unitOfWork.Repository<Role>().Query(), userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name)
            .ToList();
        var roleIds = _unitOfWork.Repository<UserRole>().Query().Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToList();
        var permissions = _unitOfWork.Repository<RolePermission>().Query()
            .Where(x => roleIds.Contains(x.RoleId))
            .Join(_unitOfWork.Repository<Permission>().Query(), rolePermission => rolePermission.PermissionId, permission => permission.Id, (_, permission) => permission.Module + ":" + permission.Action + ":" + permission.Resource)
            .Distinct()
            .ToList();
        var departmentId = _unitOfWork.Repository<Employee>().Query().Where(x => x.UserId == user.Id).Select(x => x.DepartmentId).FirstOrDefault();
        var accessToken = CreateAccessToken(user, roles, permissions, departmentId);
        return new LoginResponse(accessToken, refreshToken, AccessTokenMinutes * 60, new AuthUserDto(user.Id, user.Email, user.FullName, roles, permissions, departmentId));
    }

    private string CreateAccessToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, Guid? departmentId)
    {
        var secret = _configuration["Jwt:Secret"] ?? throw new BusinessRuleException("Jwt:Secret is not configured.");
        if (secret.Length < 32)
        {
            throw new BusinessRuleException("Jwt:Secret must be at least 32 characters.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("fullName", user.FullName)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
        if (departmentId.HasValue)
        {
            claims.Add(new Claim("departmentId", departmentId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "OmniBizAI",
            audience: _configuration["Jwt:Audience"] ?? "OmniBizAI",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static RefreshToken CreateRefreshToken(Guid userId, string? ipAddress)
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(bytes),
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
            CreatedByIp = ipAddress
        };
    }

    private async Task RecordLoginAttemptAsync(string email, string? ipAddress, bool success, string? failureReason, CancellationToken cancellationToken)
    {
        await _dbContext.UserLoginAttempts.AddAsync(new UserLoginAttempt
        {
            Email = email,
            IpAddress = ipAddress,
            IsSuccessful = success,
            FailureReason = failureReason
        }, cancellationToken);
    }
}
