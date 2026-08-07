# `partner-transaction` (v1)

Published by `Auth.Api` when a partner's balance is updated (e.g. they refer a user, or an admin processes a manual payout). Consumed by `Notification.Api` to dispatch a status email containing the ledger transaction details.

## Schema

```json
{
  "partnerEmail": "string (email)",
  "amount": "number (decimal)",
  "type": "string ('Addition' | 'Subtraction')",
  "description": "string",
  "newBalance": "number (decimal)"
}
```

## Example Payload

```json
{
  "partnerEmail": "partner@example.com",
  "amount": 2.50,
  "type": "Addition",
  "description": "Referral bonus for user registration.",
  "newBalance": 25.00
}
```

## Important Notes

*   **Routing:** The message key is the `partnerId`, ensuring per-partner ordering within Kafka partitions to maintain correct chronological balance updates.
