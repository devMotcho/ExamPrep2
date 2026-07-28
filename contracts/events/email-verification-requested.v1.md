# `email-verification-requested` (v1)

Published by `Auth.Api` when a user registers or explicitly requests an email verification code. Consumed by `Notification.Api` to dispatch an email containing the OTP code.

## Schema

```json
{
  "userId": "string (guid)",
  "email": "string (email)",
  "code": "string"
}
```

## Example Payload

```json
{
  "userId": "e6a0c5bd-705b-4a57-83f1-4dfab61c3a72",
  "email": "user@example.com",
  "code": "83491024"
}
```

## Important Notes

*   **Security:** The `code` field contains the **raw, un-hashed** 8-digit OTP so the notification service can embed it in an email. `Auth.Api` only persists the SHA-256 hash in its database. Because the outbox payload is serialized directly to Kafka, this raw code exists in the Kafka topic for the duration of the retention period.
*   **Routing:** The message key is the `userId`, ensuring per-user ordering within Kafka partitions.
