## Description
This PR combines two major feature implementations for the `Auth.Api` microservice:

1. **Partner System Implementation (`feature/partner-role`)**:
   - Adds a new `Partner` role (lifetime premium status with referral logic) adhering strictly to Clean Architecture.
   - Users can now be linked to a Partner at registration via the `PartnerEmail` field.
   - Introduces the `PartnerTransaction` entity to track monetary balances, additions, and manual subtractions (payouts).
   - Exposes new RBAC endpoints: `GET /api/partners/me` (for Partners) and `POST /api/partners/{partnerId}/subtract-balance` (for Admins).
   - Utilizes the **Outbox Pattern** to reliably queue email notifications for partner transactions (polled by Debezium and published to Kafka for a downstream Email Notification Worker).

2. **Stateless JWT Blocklist with Redis (`feature/redis-jwt-blocklist`)**:
   - Resolves the critical stateless security flaw where JWT access tokens could not be actively revoked.
   - Implements a Redis-backed distributed cache where revoked Access Token `jti` claims are stored until they naturally expire.
   - Introduces a hook in the `JwtBearerEvents.OnTokenValidated` middleware that checks the Redis cache in <1ms and rejects blocked tokens with a `401 Unauthorized`.
   - Modifies the `/logout` endpoint to push the active token `jti` to the blocklist.
   - Adds Redis to the local `docker-compose.yml` infrastructure and documents the flow in ADR `006-redis-jwt-blocklist.md`.

## Motivation and Context
- **Partner System**: Addresses the business requirement to introduce a referral and revenue-sharing system, allowing admins to track referrals and process payouts.
- **JWT Blocklist**: Ensures enterprise-grade security by allowing the system to instantly and forcefully log out compromised user sessions, closing a major loophole in stateless token architectures.

## Type of Change
- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [x] ✨ New feature (non-breaking change which adds functionality)
- [x] 🔐 Security fix/enhancement (JWT Blocklist)
- [ ] 🛠️ Refactoring (non-breaking change that improves code without altering features)
- [ ] ⚠️ Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [x] 📚 Documentation Update (adding or updating documentation)

## How Has This Been Tested?
- [ ] I have written and run Unit Tests.
- [x] I have written and run Integration Tests.
- [x] I have manually tested these changes on my local environment.

Both features have full integration test coverage via `AuthApiWebApplicationFactory` (Testcontainers):
- `PartnerControllerIntegrationTests.cs` verifies RBAC constraints and mapping.
- `LogoutEndpointTests.cs` and related tests verify that the JWT Blocklist intercepts revoked tokens and returns `401 Unauthorized` without a database hit.
- All 70 integration tests in the solution pass successfully.

## Screenshots (if applicable):
N/A (Backend APIs)

## Checklist:
- [x] My code follows the clean architecture and object-oriented design style of this project.
- [x] I have performed a self-review of my own code.
- [x] I have commented my code, particularly in hard-to-understand areas.
- [x] I have made corresponding changes to the documentation (updated README and added ADR 006).
- [x] My changes generate no new warnings or errors.
- [x] New and existing unit/integration tests pass locally with my changes.
