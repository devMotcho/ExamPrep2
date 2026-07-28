using Auth.Application.Interfaces;
using Auth.Application.Models;
using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public class PasswordResetCodeRepository(AuthDbContext db) : IPasswordResetCodeRepository
{
    /// <inheritdoc/>
    public Task AddAsync(string userId, string codeHash, DateTime expiresAt)
    {
        db.PasswordResetCodes.Add(new PasswordResetCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            Attempts = 0,
            IsUsed = false
        });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<PasswordResetCodeModel?> FindActiveByUserIdAsync(string userId)
    {
        var code = await db.PasswordResetCodes
            .Where(c => c.UserId == userId && !c.IsUsed)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        return code is null ? null
            : new PasswordResetCodeModel(code.Id, code.UserId, code.CodeHash, code.ExpiresAt, code.Attempts, code.IsUsed);
    }

    /// <inheritdoc/>
    public async Task IncrementAttemptsAsync(Guid codeId)
    {
        var code = await db.PasswordResetCodes.FindAsync(codeId);
        if (code is not null)
            code.Attempts++;
    }

    /// <inheritdoc/>
    public async Task MarkUsedAsync(Guid codeId)
    {
        var code = await db.PasswordResetCodes.FindAsync(codeId);
        if (code is not null)
            code.IsUsed = true;
    }
}
