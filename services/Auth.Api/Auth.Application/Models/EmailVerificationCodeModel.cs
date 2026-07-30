namespace Auth.Application.Models;

/// <summary>
/// Application-level view of an email verification code row.
/// Never exposes EF entity types to the Application layer.
/// </summary>
public record EmailVerificationCodeModel(
    Guid Id,
    string Email,
    string CodeHash,
    DateTime ExpiresAt,
    int Attempts,
    bool IsUsed);
