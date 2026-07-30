using Auth.Application.Interfaces;
using Auth.Application.Models;
using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public class EmailVerificationCodeRepository(AuthDbContext db) : IEmailVerificationCodeRepository
{
    /// <inheritdoc/>
    public async Task UpsertAsync(string email, string codeHash, DateTime expiresAt)
    {
        var existing = await db.EmailVerificationCodes.SingleOrDefaultAsync(c => c.Email == email);
        if (existing is not null) db.EmailVerificationCodes.Remove(existing);
        
        var code = new EmailVerificationCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            Attempts = 0,
            IsUsed = false
        };
        db.EmailVerificationCodes.Add(code);
    }

    public async Task<EmailVerificationCodeModel?> FindActiveByEmailAsync(string email)
    {
        var code = await db.EmailVerificationCodes.SingleOrDefaultAsync(c => c.Email == email && !c.IsUsed);
        return code is null ? null : new EmailVerificationCodeModel(code.Id, code.Email, code.CodeHash, code.ExpiresAt, code.Attempts, code.IsUsed);
    }

    /// <inheritdoc/>
    public async Task IncrementAttemptsAsync(Guid id)
    {
        var code = await db.EmailVerificationCodes.FindAsync(id);
        if (code is not null)
            code.Attempts++;
    }

    /// <inheritdoc/>
    public async Task MarkUsedAsync(Guid id)
    {
        var code = await db.EmailVerificationCodes.FindAsync(id);
        if (code is not null)
            code.IsUsed = true;
    }
}
