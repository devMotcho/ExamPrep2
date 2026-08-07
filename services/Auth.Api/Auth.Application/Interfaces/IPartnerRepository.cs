namespace Auth.Application.Interfaces;

public interface IPartnerRepository
{
    Task<PartnerInfoDto?> GetPartnerInfoAsync(string partnerId);
    Task<bool> AddTransactionAsync(string partnerId, decimal amount, string description);
    Task<bool> SubtractBalanceAsync(string partnerId, decimal amount, string description);
}
