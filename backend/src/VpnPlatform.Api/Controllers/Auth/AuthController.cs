using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Api.Contracts;
using VpnPlatform.Api.Security;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;
    private readonly IConfiguration _configuration;

    public AuthController(
        IApplicationDbContext db,
        IPasswordService passwordService,
        ITokenService tokenService,
        IClock clock,
        IConfiguration configuration)
    {
        _db = db;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _clock = clock;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [EnableRateLimiting(ApiRateLimitPolicies.AuthSensitive)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request?.Email);
        var password = request?.Password ?? string.Empty;
        var displayName = NormalizeDisplayName(request?.DisplayName, normalizedEmail);
        if (!IsValidEmail(normalizedEmail) || string.IsNullOrWhiteSpace(password) || password.Trim().Length < 8 || displayName.Length > 80)
        {
            return BadRequest(new { error = "invalid_registration_request" });
        }

        var exists = await _db.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            return BadRequest(new { error = "email_exists" });
        }

        var user = new User
        {
            Email = normalizedEmail,
            DisplayName = displayName,
            PasswordHash = _passwordService.Hash(password),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = $"REF-{Guid.NewGuid():N}"[..10]
        };

        _db.Users.Add(user);
        AddAudit("auth.register", "User", user.Id, null, new { email = normalizedEmail });
        var response = await IssueAuthResponseAsync(user, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPost("login")]
    [EnableRateLimiting(ApiRateLimitPolicies.AuthSensitive)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request?.Email);
        var password = request?.Password ?? string.Empty;
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (user is null || user.IsBlocked || user.Status != UserStatus.Active || string.IsNullOrWhiteSpace(password) || !_passwordService.Verify(password, user.PasswordHash))
        {
            return Unauthorized(new { error = "invalid_credentials" });
        }

        user.LastLoginAt = _clock.UtcNow;
        AddAudit("auth.login", "User", user.Id, null, new { email = normalizedEmail });
        var response = await IssueAuthResponseAsync(user, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var token = request?.RefreshToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            var hash = HashToken(token);
            var session = await _db.UserRefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
            if (session is not null && session.RevokedAt is null)
            {
                session.RevokedAt = now;
                session.RevokedByIp = ResolveIp();
                session.RevocationReason = "logout";
                AddAudit("auth.logout", "UserRefreshToken", session.Id, null, new { session.UserId });
            }
        }
        else if (ResolveUserId() is { } userId && userId != Guid.Empty)
        {
            var activeSessions = await _db.UserRefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > now)
                .ToListAsync(cancellationToken);
            foreach (var session in activeSessions)
            {
                session.RevokedAt = now;
                session.RevokedByIp = ResolveIp();
                session.RevocationReason = "logout_all_current_user";
            }
            if (activeSessions.Count > 0)
            {
                AddAudit("auth.logout_all", "User", userId, null, new { sessionsRevoked = activeSessions.Count });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "ok" });
    }

    [HttpPost("refresh")]
    [EnableRateLimiting(ApiRateLimitPolicies.AuthSensitive)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            return Unauthorized(new { error = "invalid_refresh_token" });
        }

        var now = _clock.UtcNow;
        var hash = HashToken(request.RefreshToken);
        var session = await _db.UserRefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (session is null)
        {
            return Unauthorized(new { error = "invalid_refresh_token" });
        }

        if (session.RevokedAt is not null)
        {
            session.ReuseDetectedAt ??= now;
            AddAudit("auth.refresh_reuse_detected", "User", session.UserId, null, new { session.Id });
            var currentUser = await _db.Users.FirstOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
            if (currentUser is null || currentUser.IsBlocked || currentUser.Status != UserStatus.Active)
            {
                await _db.SaveChangesAsync(cancellationToken);
                return Unauthorized(new { error = "user_not_active" });
            }

            if (session.SessionVersion != currentUser.SessionVersion)
            {
                await _db.SaveChangesAsync(cancellationToken);
                return Unauthorized(new { error = "session_invalidated" });
            }

            await RevokeRefreshFamilyAsync(session, "refresh_reuse_detected", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return Unauthorized(new { error = "refresh_token_reuse_detected" });
        }

        if (session.ExpiresAt <= now)
        {
            session.RevokedAt = now;
            session.RevokedByIp = ResolveIp();
            session.RevocationReason = "expired";
            await _db.SaveChangesAsync(cancellationToken);
            return Unauthorized(new { error = "refresh_token_expired" });
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
        if (user is null || user.IsBlocked || user.Status != UserStatus.Active)
        {
            session.RevokedAt = now;
            session.RevokedByIp = ResolveIp();
            session.RevocationReason = "user_not_active";
            await _db.SaveChangesAsync(cancellationToken);
            return Unauthorized(new { error = "user_not_active" });
        }

        if (session.SessionVersion != user.SessionVersion)
        {
            session.RevokedAt = now;
            session.RevokedByIp = ResolveIp();
            session.RevocationReason = "session_version_mismatch";
            await _db.SaveChangesAsync(cancellationToken);
            return Unauthorized(new { error = "session_invalidated" });
        }

        var rawRefreshToken = _tokenService.CreateRefreshToken();
        var newHash = HashToken(rawRefreshToken);
        session.RevokedAt = now;
        session.RevokedByIp = ResolveIp();
        session.RevocationReason = "rotated";
        session.ReplacedByTokenHash = newHash;
        var familyId = session.FamilyId ?? session.Id;
        session.FamilyId = familyId;
        _db.UserRefreshTokens.Add(BuildRefreshToken(user, rawRefreshToken, familyId));
        user.LastLoginAt = now;
        AddAudit("auth.refresh", "User", user.Id, null, new { sessionRotated = session.Id });
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AuthResponse(
            _tokenService.CreateAccessToken(user, UserRoles.Parse(user.RolesCsv)),
            rawRefreshToken,
            user.Email ?? string.Empty,
            user.DisplayName));
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(ApiRateLimitPolicies.AuthSensitive)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request?.Email);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        string? validationToken = null;

        if (user is not null && !user.IsBlocked && user.Status == UserStatus.Active)
        {
            var rawToken = _tokenService.CreateRefreshToken();
            validationToken = _configuration.GetValue<bool>("Auth:PasswordReset:ReturnTokenForValidation") ? rawToken : null;
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = HashToken(rawToken),
                ExpiresAt = _clock.UtcNow.AddMinutes(GetInt("Auth:PasswordReset:ExpiryMinutes", 30)),
                RequestedByIp = ResolveIp(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };
            _db.PasswordResetTokens.Add(resetToken);
            _db.OutboxMessages.Add(new OutboxMessage
            {
                Type = "password_reset_requested",
                CorrelationId = resetToken.Id.ToString("N"),
                PayloadJson = JsonSerializer.Serialize(new { userId = user.Id, email = user.Email, validationTokenReturned = validationToken is not null })
            });
            AddAudit("auth.password_reset_requested", "User", user.Id, null, new { email = normalizedEmail, validationTokenReturned = validationToken is not null });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new ForgotPasswordResponse(true, "If the account exists, a password reset instruction has been queued for the configured delivery channel.", validationToken));
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(ApiRateLimitPolicies.AuthSensitive)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var token = request?.Token ?? string.Empty;
        var newPassword = request?.NewPassword ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword) || newPassword.Trim().Length < 8)
        {
            return BadRequest(new { error = "invalid_reset_request" });
        }

        var now = _clock.UtcNow;
        var hash = HashToken(token);
        var reset = await _db.PasswordResetTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (reset is null || reset.UsedAt is not null || reset.InvalidatedAt is not null || reset.ExpiresAt <= now)
        {
            return BadRequest(new { error = "invalid_or_expired_reset_token" });
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == reset.UserId, cancellationToken);
        if (user is null || user.IsBlocked || user.Status != UserStatus.Active)
        {
            return BadRequest(new { error = "invalid_or_expired_reset_token" });
        }

        user.PasswordHash = _passwordService.Hash(newPassword);
        user.SessionVersion = checked(user.SessionVersion + 1);
        user.UpdatedAt = now;
        reset.UsedAt = now;
        reset.Revision = checked(reset.Revision + 1);
        reset.UpdatedAt = now;
        var siblingTokens = await _db.PasswordResetTokens
            .Where(x => x.UserId == user.Id
                && x.Id != reset.Id
                && x.UsedAt == null
                && x.InvalidatedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var sibling in siblingTokens)
        {
            sibling.InvalidatedAt = now;
            sibling.InvalidationReason = "password_reset_completed";
            sibling.Revision = checked(sibling.Revision + 1);
            sibling.UpdatedAt = now;
        }
        await RevokeUserSessionsAsync(user.Id, "password_reset", cancellationToken);
        AddAudit("auth.password_reset_completed", "User", user.Id, null, new { resetTokenId = reset.Id, siblingTokensInvalidated = siblingTokens.Count });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (_db is DbContext dbContext)
            {
                dbContext.ChangeTracker.Clear();
            }
            return BadRequest(new { error = "invalid_or_expired_reset_token" });
        }
        return Ok(new { status = "password_changed" });
    }

    private async Task<AuthResponse> IssueAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var refreshToken = _tokenService.CreateRefreshToken();
        _db.UserRefreshTokens.Add(BuildRefreshToken(user, refreshToken, Guid.NewGuid()));
        await Task.CompletedTask;
        return new AuthResponse(
            _tokenService.CreateAccessToken(user, UserRoles.Parse(user.RolesCsv)),
            refreshToken,
            user.Email ?? string.Empty,
            user.DisplayName);
    }

    private UserRefreshToken BuildRefreshToken(User user, string rawToken, Guid familyId)
        => new()
        {
            UserId = user.Id,
            SessionVersion = user.SessionVersion,
            FamilyId = familyId,
            TokenHash = HashToken(rawToken),
            ExpiresAt = _clock.UtcNow.AddDays(GetInt("Auth:RefreshTokenDays", 30)),
            CreatedByIp = ResolveIp(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

    private async Task RevokeRefreshFamilyAsync(UserRefreshToken reusedSession, string reason, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var familyId = reusedSession.FamilyId ?? reusedSession.Id;
        reusedSession.FamilyId = familyId;
        var visited = new HashSet<Guid> { reusedSession.Id };
        var nextHash = reusedSession.ReplacedByTokenHash;
        while (!string.IsNullOrWhiteSpace(nextHash))
        {
            var descendant = await _db.UserRefreshTokens.FirstOrDefaultAsync(
                x => x.UserId == reusedSession.UserId
                    && x.SessionVersion == reusedSession.SessionVersion
                    && x.TokenHash == nextHash,
                cancellationToken);
            if (descendant is null || !visited.Add(descendant.Id))
            {
                break;
            }

            descendant.FamilyId = familyId;
            if (descendant.RevokedAt is null)
            {
                descendant.RevokedAt = now;
                descendant.RevokedByIp = ResolveIp();
                descendant.RevocationReason = reason;
            }
            nextHash = descendant.ReplacedByTokenHash;
        }

        var sessions = await _db.UserRefreshTokens
            .Where(x => x.UserId == reusedSession.UserId
                && x.SessionVersion == reusedSession.SessionVersion
                && x.FamilyId == familyId
                && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAt = now;
            session.RevokedByIp = ResolveIp();
            session.RevocationReason = reason;
        }
    }

    private async Task RevokeUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var sessions = await _db.UserRefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAt = now;
            session.RevokedByIp = ResolveIp();
            session.RevocationReason = reason;
        }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToBase64String(bytes);
    }

    private void AddAudit(string action, string entityType, Guid entityId, object? before, object after)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorType = "auth",
            ActorId = ResolveUserId()?.ToString() ?? entityId.ToString(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            BeforeJson = SensitiveDataRedactor.Redact(before is null ? "{}" : JsonSerializer.Serialize(before)),
            AfterJson = SensitiveDataRedactor.Redact(JsonSerializer.Serialize(after)),
            Ip = ResolveIp(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });
    }

    private int GetInt(string key, int fallback)
        => int.TryParse(_configuration[key], out var value) && value > 0 ? value : fallback;

    private Guid? ResolveUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var value) ? value : null;
    }

    private string ResolveIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(email);
            var domain = email.Split('@', 2).Length == 2 ? email.Split('@', 2)[1] : string.Empty;
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase)
                && domain.Contains('.', StringComparison.Ordinal)
                && !email.Contains(' ', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeDisplayName(string? displayName, string normalizedEmail)
    {
        var normalized = (displayName ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        var localPart = normalizedEmail.Split('@', 2)[0].Trim();
        return string.IsNullOrWhiteSpace(localPart) ? "User" : localPart;
    }
}
