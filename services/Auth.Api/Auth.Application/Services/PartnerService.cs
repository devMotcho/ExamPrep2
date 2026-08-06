using Auth.Application.Interfaces;

namespace Auth.Application.Services;

public class PartnerService(IPartnerRepository partnerRepository) : IPartnerService
{
    public Task<PartnerInfoDto?> GetPartnerInfoAsync(string partnerId)
    {
        return partnerRepository.GetPartnerInfoAsync(partnerId);
    }

    public Task<bool> AddTransactionAsync(string partnerId, decimal amount, string description)
    {
        return partnerRepository.AddTransactionAsync(partnerId, amount, description);
    }

    public Task<bool> SubtractBalanceAsync(string partnerId, decimal amount, string description)
    {
        return partnerRepository.SubtractBalanceAsync(partnerId, amount, description);
    }
}
