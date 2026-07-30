# Email Verification Flow

This document details the architecture, design decisions, and sequence flow for the two-step email verification feature implemented in `Auth.Api`.

## 1. Overview and Architecture

The email verification feature ensures that a user owns the email address they registered with. It is highly similar in design to the Password Reset flow, prioritizing security against timing and brute-force attacks.

### The Two-Step Process

1.  **Request (`POST /api/auth/email-verification/request`)**
    *   Generates an 8-digit OTP code, hashes it using SHA-256, and stores it in the database (`EmailVerificationCode`).
    *   Publishes an `email-verification-requested` event to the transactional outbox, containing the raw code.
    *   `Notification.Api` consumes this event via Kafka and emails the user.
    *   **Security:** Always returns `200 OK` for unknown emails to prevent account enumeration. If the user is already verified, it gracefully returns a specific "AlreadyVerified" status.

2.  **Verify (`POST /api/auth/email-verification/verify`)**
    *   Validates the user's submitted 8-digit code against the stored hash.
    *   **Security:** Uses constant-time comparison (`CryptographicOperations.FixedTimeEquals`) to prevent timing attacks. Enforces a maximum of 5 failed attempts before locking the code.
    *   On success, burns the OTP code (marks it `IsUsed = true`) and marks the user's email as confirmed via ASP.NET Core Identity (`EmailConfirmed = true`).

## 2. Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Client as SPA / Client
    participant AuthApi as Auth.Api
    participant DB as Postgres (AuthDB)
    participant Kafka
    participant NotifApi as Notification.Api

    %% Step 1: Request
    rect rgb(240, 248, 255)
    Note over User, NotifApi: 1. Request Verification
    User->>Client: Clicks "Verify Email"
    Client->>AuthApi: POST /request { email }
    AuthApi->>DB: Find user by email
    AuthApi-->>AuthApi: Generate 8-digit rawCode & codeHash
    
    rect rgb(240, 255, 240)
    Note over AuthApi, DB: Transaction
    AuthApi->>DB: Insert EmailVerificationCode (codeHash)
    AuthApi->>DB: Insert OutboxMessage (EmailVerificationRequestedEvent + rawCode)
    end
    
    AuthApi-->>Client: 200 OK (Always, anti-enumeration)
    end

    %% Async delivery
    DB-->>Kafka: Debezium CDC reads Outbox
    Kafka-->>NotifApi: Consumes event
    NotifApi->>User: Sends email with rawCode

    %% Step 2: Verify
    rect rgb(255, 248, 240)
    Note over User, AuthApi: 2. Verify Code
    User->>Client: Reads email, types 8-digit code
    Client->>AuthApi: POST /verify { email, code }
    AuthApi->>DB: Query active code for user
    AuthApi-->>AuthApi: Constant-time hash compare
    
    alt Correct Code
        rect rgb(240, 255, 240)
        Note over AuthApi, DB: Transaction
        AuthApi->>DB: Mark EmailVerificationCode IsUsed = true
        AuthApi->>DB: UserManager.ConfirmEmailAsync (sets EmailConfirmed = true)
        end
        AuthApi-->>Client: 200 OK
    else Wrong Code
        AuthApi->>DB: Increment Attempts
        AuthApi-->>Client: 400 Bad Request
    end
    end
```

## 3. Entity Models

### EmailVerificationCode
*   `CodeHash`: SHA-256 hash of the 8-digit OTP.
*   `Attempts`: Integer tracking failed `/verify` attempts.
*   `IsUsed`: Boolean marking the code as burnt after a successful verify.
*   `ExpiresAt`: Timestamp (default 15 minutes TTL).
