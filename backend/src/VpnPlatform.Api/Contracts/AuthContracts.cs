namespace VpnPlatform.Api.Contracts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, string RefreshToken, string Email, string DisplayName);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string? RefreshToken = null);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ForgotPasswordResponse(bool Accepted, string Message, string? ValidationResetToken = null);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
