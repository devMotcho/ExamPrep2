# OAuth Account Linking Policy

This document details the architectural decisions and security policies around linking third-party OAuth identities (e.g., Google) to existing accounts in `Auth.Api`.

## 1. The Security Problem

Initially, the system implemented an "auto-linking" policy: if a user authenticated via Google and the email address provided by Google matched an existing account in our database, the system automatically linked the Google identity to the existing account and issued tokens.

**The Vulnerability:**
This approach allowed a straightforward account-takeover vector. If an attacker managed to register a Google account with an unverified email matching a victim's account in our system (or if Google's `email_verified` claim was ignored), they could log in via Google and instantly gain access to the victim's existing account. Even if `email_verified` is checked, silently linking a third-party login to an existing password-based account without explicit consent violates the principle of least privilege and informed consent. 

## 2. The Solution: Proof of Ownership

We refactored the OAuth login flow to introduce a "require confirmation" policy. The core principle is: **linking a third-party identity to an existing account requires proving control over the existing account via its own credential (e.g., password).**

### 2.1. The Flow

1. **OAuth Login Attempt:** The user authenticates via a provider (e.g., Google).
2. **Identity Verification:** We check the `email_verified` claim. If false, we reject the login.
3. **Collision Detection:**
    * If the provider identity is already linked to a user, we issue tokens immediately (Fast Path).
    * If no user exists with the matching email, we create a new user and link the identity immediately (Safe Path, no pre-existing account at risk).
    * **If a user exists with the matching email but the identity is not linked:** We halt the login process.
4. **Pending Link Ticket:** We generate a short-lived (10 minutes) cryptographic ticket (`PendingOAuthLink`) and store its hash in the database.
5. **Client Prompt:** The API returns a `200 OK` with an `AccountLinkRequired` status, a masked version of the email, and the raw link ticket. The frontend must prompt the user: *"An account with this email exists — sign in with your password to link Google."*
6. **Confirmation:** The frontend sends a `POST` request to `/api/auth/oauth/link/confirm` with the ticket and the user's existing account password.
7. **Linking & Issuance:** If the ticket is valid and the password is correct, we finally add the login identity to the user's account and issue the access/refresh tokens.

### 2.2. Architectural Implementation

*   **`PendingOAuthLink` Entity:** Lives in `Auth.Infrastructure` since it is persistence-shaped state tied to the Identity flow. It tracks the `UserId`, `Provider`, `ProviderKey`, `TicketHash`, and expiry.
*   **Ticket Primitive:** We reuse the `ITokenService.GenerateResetTicket()` method (originally built for password resets) to generate a 64-byte URL-safe cryptographic ticket, ensuring consistency in our security primitives.
*   **Constant-Time Hashing:** The ticket is hashed via SHA-256 before being stored in the database, preventing ticket theft if the database is compromised.
*   **Idempotency & Replay Protection:** Once a ticket is successfully used to link an account, its `IsUsed` flag is set to `true`, preventing replay attacks.

### 2.3. Brute Force Protection

To prevent attackers from using the account link confirmation endpoint as a password-guessing oracle, the flow integrates tightly with our core brute force protections:

1. **Per-Ticket Attempt Cap (`Attempts` property):**
   Every failed password guess on a `PendingOAuthLink` increments its `Attempts` counter. If the counter reaches `5`, the ticket is permanently disabled, even if it hasn't expired yet. This bounds the number of guesses an attacker can make per OAuth login initiation.

2. **Global Identity Lockout Integration:**
   Since the confirmation endpoint verifies the user's password, failed attempts are also forwarded to ASP.NET Core Identity's global lockout system (`UserManager.AccessFailedAsync`).
   - If a user accumulates 5 failed password attempts (whether through standard login or OAuth linking), their account is locked for 15 minutes.
   - The confirmation endpoint explicitly checks `IsLockedOutAsync` and halts immediately if the account is locked, returning `InvalidPassword` to obscure the lockout state from attackers.
   - We enforce `Lockout.AllowedForNewUsers = true` globally, ensuring this protection applies automatically to all accounts created in the system.
