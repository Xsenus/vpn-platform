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
    private readonly ISecretProtector _secretProtector;

    public AuthController(
        IApplicationDbContext db,
        IPasswordService passwordService,
        ITokenService tokenService,
        IClock clock,
        IConfiguration configuration,
        ISecretProtector secretProtector)
    {
        _db = db;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _clock = clock;
        _configuration = configuration;
        _secretProtector = secretProtector;
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

        User? referrer = null;
        var normalizedReferralCode = NormalizeReferralCode(request?.ReferralCode);
        if (normalizedReferralCode is not null)
        {
            referrer = await _db.Users.AsNoTracking().FirstOrDefaultAsync(
                x => x.ReferralCode.ToUpper() == normalizedReferralCode
                    && x.Status == UserStatus.Active
                    && !x.IsBlocked,
                cancellationToken);
            if (referrer is null)
            {
                return BadRequest(new { error = "invalid_referral_code" });
            }
        }

        var user = new User
        {
            Email = normalizedEmail,
            DisplayName = displayName,
            PasswordHash = _passwordService.Hash(password),
            RolesCsv = UserRoles.User,
            Status = UserStatus.Active,
            ReferralCode = CreateReferralCode()
        };

        _db.Users.Add(user);
        if (referrer is not null)
        {
            _db.ReferralRelationships.Add(new ReferralRelationship
            {
                ReferrerUserId = referrer.Id,
                ReferredUserId = user.Id,
                SourceChannel = ChannelType.Web
            });
        }
        AddAudit("auth.register", "User", user.Id, null, new { email = normalizedEmail });
        var response = await IssueAuthResponseAsync(user, cancellationToken);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUserEmailUniqueConstraintViolation(ex))
        {
            if (_db is DbContext dbContext)
            {
                dbContext.ChangeTracker.Clear();
            }
            return BadRequest(new { error = "email_exists" });
        }

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
        var token = request?.RefreshToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            return await LogoutRefreshFamilyAsync(HashToken(token), cancellationToken);
        }

        if (ResolveUserId() is not { } userId || userId == Guid.Empty)
        {
            return Ok(new { status = "ok" });
        }

        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var now = _clock.UtcNow;
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user is null)
            {
                return Ok(new { status = "ok" });
            }

            user.SessionVersion = checked(user.SessionVersion + 1);
            user.UpdatedAt = now;
            var activeSessions = await _db.UserRefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var session in activeSessions)
            {
                RevokeRefreshSession(session, "logout_all_current_user", now);
            }

            AddAudit("auth.logout_all", "User", userId, null, new { sessionsRevoked = activeSessions.Count });
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return Ok(new { status = "ok" });
            }
            catch (DbUpdateConcurrencyException)
            {
                ClearChangeTracker();
            }
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "logout_retry_required" });
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
            return await RejectRefreshReuseAsync(hash, cancellationToken);
        }

        if (session.ExpiresAt <= now)
        {
            RevokeRefreshSession(session, "expired", now);
            await TrySaveRefreshChangesAsync(cancellationToken);
            return Unauthorized(new { error = "refresh_token_expired" });
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
        if (user is null || user.IsBlocked || user.Status != UserStatus.Active)
        {
            RevokeRefreshSession(session, "user_not_active", now);
            await TrySaveRefreshChangesAsync(cancellationToken);
            return Unauthorized(new { error = "user_not_active" });
        }

        if (session.SessionVersion != user.SessionVersion)
        {
            RevokeRefreshSession(session, "session_version_mismatch", now);
            await TrySaveRefreshChangesAsync(cancellationToken);
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
        TouchRefreshSession(session, now);
        _db.UserRefreshTokens.Add(BuildRefreshToken(user, rawRefreshToken, familyId));
        user.LastLoginAt = now;
        AddAudit("auth.refresh", "User", user.Id, null, new { sessionRotated = session.Id });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearChangeTracker();
            return await RejectRefreshReuseAsync(hash, cancellationToken);
        }

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
        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
            if (user is null || user.IsBlocked || user.Status != UserStatus.Active)
            {
                return AcceptedForgotPasswordResponse(null);
            }

            var now = _clock.UtcNow;
            var state = await _db.PasswordResetStates.FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
            if (state is null)
            {
                state = new PasswordResetState { UserId = user.Id, Generation = 1 };
                _db.PasswordResetStates.Add(state);
            }
            else
            {
                state.Generation = checked(state.Generation + 1);
                state.Revision = checked(state.Revision + 1);
                state.UpdatedAt = now;
            }

            var supersededTokens = await _db.PasswordResetTokens
                .Where(x => x.UserId == user.Id && x.UsedAt == null && x.InvalidatedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var superseded in supersededTokens)
            {
                superseded.InvalidatedAt = now;
                superseded.InvalidationReason = "password_reset_reissued";
                superseded.Revision = checked(superseded.Revision + 1);
                superseded.UpdatedAt = now;
            }

            var rawToken = _tokenService.CreateRefreshToken();
            var expiryMinutes = GetInt("Auth:PasswordReset:ExpiryMinutes", 30);
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Generation = state.Generation,
                TokenHash = HashToken(rawToken),
                ExpiresAt = now.AddMinutes(expiryMinutes),
                RequestedByIp = ResolveIp(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };
            _db.PasswordResetTokens.Add(resetToken);
            var returnValidationToken = _configuration.GetValue<bool>("Auth:PasswordReset:ReturnTokenForValidation");
            _db.OutboxMessages.Add(new OutboxMessage
            {
                Type = "password_reset_requested",
                CorrelationId = resetToken.Id.ToString("N"),
                PayloadJson = JsonSerializer.Serialize(new
                {
                    userId = user.Id,
                    email = user.Email,
                    protectedResetToken = _secretProtector.Protect(rawToken),
                    expiryMinutes,
                    validationTokenReturned = returnValidationToken
                })
            });
            AddAudit("auth.password_reset_requested", "User", user.Id, null, new
            {
                email = normalizedEmail,
                generation = state.Generation,
                supersededTokens = supersededTokens.Count,
                validationTokenReturned = returnValidationToken
            });

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return AcceptedForgotPasswordResponse(returnValidationToken ? rawToken : null);
            }
            catch (DbUpdateConcurrencyException)
            {
                ClearChangeTracker();
                if (attempt == maxAttempts - 1)
                {
                    return AcceptedForgotPasswordResponse(null);
                }
            }
            catch (DbUpdateException ex) when (IsPasswordResetStateUniqueConstraintViolation(ex))
            {
                ClearChangeTracker();
                if (attempt == maxAttempts - 1)
                {
                    return AcceptedForgotPasswordResponse(null);
                }
            }
        }

        return AcceptedForgotPasswordResponse(null);
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

        var resetState = await _db.PasswordResetStates.FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
        if (reset.Generation != (resetState?.Generation ?? 0))
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
        if (resetState is null)
        {
            _db.PasswordResetStates.Add(new PasswordResetState
            {
                UserId = user.Id,
                Generation = 1
            });
        }
        else
        {
            resetState.Generation = checked(resetState.Generation + 1);
            resetState.Revision = checked(resetState.Revision + 1);
            resetState.UpdatedAt = now;
        }
        await RevokeUserSessionsAsync(user.Id, "password_reset", cancellationToken);
        AddAudit("auth.password_reset_completed", "User", user.Id, null, new { resetTokenId = reset.Id, siblingTokensInvalidated = siblingTokens.Count });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearChangeTracker();
            return BadRequest(new { error = "invalid_or_expired_reset_token" });
        }
        catch (DbUpdateException ex) when (IsPasswordResetStateUniqueConstraintViolation(ex))
        {
            ClearChangeTracker();
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

    private async Task<IActionResult> LogoutRefreshFamilyAsync(string tokenHash, CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var session = await _db.UserRefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
            if (session is null)
            {
                return Ok(new { status = "ok" });
            }

            var now = _clock.UtcNow;
            RevokeRefreshSession(session, "logout", now);
            await RevokeRefreshFamilyAsync(session, "logout", cancellationToken);
            AddAudit("auth.logout", "UserRefreshToken", session.Id, null, new { session.UserId });
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return Ok(new { status = "ok" });
            }
            catch (DbUpdateConcurrencyException)
            {
                ClearChangeTracker();
            }
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "logout_retry_required" });
    }

    private async Task<IActionResult> RejectRefreshReuseAsync(string tokenHash, CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            ClearChangeTracker();
            var session = await _db.UserRefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
            if (session is null)
            {
                return Unauthorized(new { error = "invalid_refresh_token" });
            }

            var now = _clock.UtcNow;
            if (session.ReuseDetectedAt is null)
            {
                session.ReuseDetectedAt = now;
                TouchRefreshSession(session, now);
            }
            AddAudit("auth.refresh_reuse_detected", "User", session.UserId, null, new { session.Id });

            var currentUser = await _db.Users.FirstOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
            string error;
            if (currentUser is null || currentUser.IsBlocked || currentUser.Status != UserStatus.Active)
            {
                error = "user_not_active";
            }
            else if (session.SessionVersion != currentUser.SessionVersion)
            {
                error = "session_invalidated";
            }
            else
            {
                await RevokeRefreshFamilyAsync(session, "refresh_reuse_detected", cancellationToken);
                error = "refresh_token_reuse_detected";
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return Unauthorized(new { error });
            }
            catch (DbUpdateConcurrencyException)
            {
                ClearChangeTracker();
            }
        }

        return Unauthorized(new { error = "refresh_token_reuse_detected" });
    }

    private async Task<bool> TrySaveRefreshChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearChangeTracker();
            return false;
        }
    }

    private async Task RevokeRefreshFamilyAsync(UserRefreshToken reusedSession, string reason, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var familyId = reusedSession.FamilyId ?? reusedSession.Id;
        if (reusedSession.FamilyId != familyId)
        {
            reusedSession.FamilyId = familyId;
            TouchRefreshSession(reusedSession, now);
        }
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

            var changed = false;
            if (descendant.FamilyId != familyId)
            {
                descendant.FamilyId = familyId;
                changed = true;
            }
            if (descendant.RevokedAt is null)
            {
                descendant.RevokedAt = now;
                descendant.RevokedByIp = ResolveIp();
                descendant.RevocationReason = reason;
                changed = true;
            }
            if (changed)
            {
                TouchRefreshSession(descendant, now);
            }
            nextHash = descendant.ReplacedByTokenHash;
        }

        var sessions = await _db.UserRefreshTokens
            .Where(x => x.UserId == reusedSession.UserId
                && x.SessionVersion == reusedSession.SessionVersion
                && x.FamilyId == familyId
                && !visited.Contains(x.Id)
                && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            RevokeRefreshSession(session, reason, now);
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
            RevokeRefreshSession(session, reason, now);
        }
    }

    private void RevokeRefreshSession(UserRefreshToken session, string reason, DateTimeOffset now)
    {
        if (session.RevokedAt is not null)
        {
            return;
        }

        session.RevokedAt = now;
        session.RevokedByIp = ResolveIp();
        session.RevocationReason = reason;
        TouchRefreshSession(session, now);
    }

    private static void TouchRefreshSession(UserRefreshToken session, DateTimeOffset now)
    {
        session.Revision = checked(session.Revision + 1);
        session.UpdatedAt = now;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToBase64String(bytes);
    }

    private static string CreateReferralCode()
        => $"REF-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}";

    private static string? NormalizeReferralCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static bool IsUserEmailUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            var constraintName = current.GetType().GetProperty("ConstraintName")?.GetValue(current)?.ToString();
            if (string.Equals(constraintName, "IX_Users_Email", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("Users.Email", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPasswordResetStateUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            var constraintName = current.GetType().GetProperty("ConstraintName")?.GetValue(current)?.ToString();
            if (string.Equals(constraintName, "IX_PasswordResetStates_UserId", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("IX_PasswordResetStates_UserId", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("PasswordResetStates.UserId", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private IActionResult AcceptedForgotPasswordResponse(string? validationToken)
        => Ok(new ForgotPasswordResponse(
            true,
            "If the account exists, a password reset instruction has been queued for the configured delivery channel.",
            validationToken));

    private void ClearChangeTracker()
    {
        if (_db is DbContext dbContext)
        {
            dbContext.ChangeTracker.Clear();
        }
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
