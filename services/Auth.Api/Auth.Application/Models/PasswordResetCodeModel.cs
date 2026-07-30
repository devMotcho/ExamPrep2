namespace Auth.Application.Models;

/// <summary>
/// Application-level view of a password-reset OTP row.
/// Never exposes EF entity types to the Application layer.
/// </summary>
public record PasswordResetCodeModel(
    Guid Id,
    string UserId,
    string CodeHash,
    DateTime ExpiresAt,
    int Attempts,
    bool IsUsed);
