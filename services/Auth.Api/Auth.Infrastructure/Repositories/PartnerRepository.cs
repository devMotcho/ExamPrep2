using Auth.Application.Interfaces;
using Auth.Infrastructure.Identity;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Auth.Application.Events;
using System.Text.Json;
using ExamPrep.Shared.Constants;

namespace Auth.Infrastructure.Repositories;

public class PartnerRepository(AuthDbContext dbContext, IOutboxRepository outbox) : IPartnerRepository
{
    public async Task<PartnerInfoDto?> GetPartnerInfoAsync(string partnerId)
    {
        var partner = await dbContext.Users
            .Include(u => u.PartnerTransactions)
            .FirstOrDefaultAsync(u => u.Id == partnerId);

        if (partner is null) return null;

        var transactions = partner.PartnerTransactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new PartnerTransactionDto(t.Id, t.Amount, (int)t.Type, t.CreatedAt, t.Description))
            .ToList();

        return new PartnerInfoDto(partner.Email!, partner.PartnerBalance, transactions);
    }

    public async Task<bool> AddTransactionAsync(string partnerId, decimal amount, string description)
    {
        if (amount <= 0) return false;

        var partner = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == partnerId);
        if (partner is null) return false;

        partner.PartnerBalance += amount;
        
        var transaction = new PartnerTransaction
        {
            Id = Guid.NewGuid(),
            PartnerId = partnerId,
            Amount = amount,
            Type = TransactionType.Addition,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
        
        dbContext.PartnerTransactions.Add(transaction);
        
        var evt = new PartnerTransactionEvent(
            partner.Email!, amount, "Addition", description, partner.PartnerBalance);
            
        await outbox.AddAsync(KafkaTopics.PartnerTransaction, partnerId, JsonSerializer.Serialize(evt));
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SubtractBalanceAsync(string partnerId, decimal amount, string description)
    {
        if (amount <= 0) return false;

        var partner = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == partnerId);
        if (partner is null) return false;

        if (partner.PartnerBalance < amount) return false; // Insufficient funds

        partner.PartnerBalance -= amount;

        var transaction = new PartnerTransaction
        {
            Id = Guid.NewGuid(),
            PartnerId = partnerId,
            Amount = amount,
            Type = TransactionType.Subtraction,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PartnerTransactions.Add(transaction);
        
        var evt = new PartnerTransactionEvent(
            partner.Email!, amount, "Subtraction", description, partner.PartnerBalance);
            
        await outbox.AddAsync(KafkaTopics.PartnerTransaction, partnerId, JsonSerializer.Serialize(evt));
        await dbContext.SaveChangesAsync();

        return true;
    }
}
