namespace Auth.Application.Events;

public record PartnerTransactionEvent(
    string PartnerEmail,
    decimal Amount,
    string TransactionType,
    string Description,
    decimal NewBalance);
