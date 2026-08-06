using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Auth.Application.Interfaces;

public record PartnerTransactionDto(Guid Id, decimal Amount, int Type, DateTime CreatedAt, string Description);
public record PartnerInfoDto(string PartnerEmail, decimal Balance, IReadOnlyList<PartnerTransactionDto> Transactions);

public interface IPartnerService
{
    Task<PartnerInfoDto?> GetPartnerInfoAsync(string partnerId);
    Task<bool> AddTransactionAsync(string partnerId, decimal amount, string description);
    Task<bool> SubtractBalanceAsync(string partnerId, decimal amount, string description);
}
