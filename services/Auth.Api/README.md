
# Auth.Api Project Structure

```text
Auth.Domain/
├── ValueObjects/       (Email, HashedPassword, etc. if you want them)
└── Rules/              (token expiry rules, password policy — plain logic, no dependencies)

Auth.Infrastructure/
├── Identity/          (User, RefreshToken — ASP.NET Core Identity entities)
├── Persistence/
│   ├── AuthDbContext.cs
│   ├── IUnitOfWork.cs
│   ├── UnitOfWork.cs
│   ├── Migrations/
│   └── Outbox/          (OutboxMessage entity — no relay code, CDC-based delivery)
├── Repositories/       (IUserRepository, IRefreshTokenRepository, IOutboxRepository + impls)
├── Security/           (ITokenService, JwtTokenService, RsaKeyProvider)
└── Messaging/          (UserRegisteredEvent)

Auth.Application/              ← orchestration / use-case layer
├── Results/
│   └── RegisterResult.cs      (RegisterStatus enum + result factory methods)
└── Services/
    ├── IAuthService.cs
    └── AuthService.cs         (RegisterAsync: transaction, repos, outbox, token generation)

Auth.Api/
├── Controllers/          (AuthController — thin HTTP adapter only; JwksController)
├── Contracts/            (RegisterRequest, AuthResponse DTOs)
├── Program.cs
└── appsettings.json

Auth.Api.Tests/
├── Fixtures/
├── IntegrationTests/
└── UnitTests/
```

## Dependency direction

```
Auth.Api → Auth.Application → Auth.Infrastructure → Auth.Domain
```

`Auth.Api` retains a direct reference to `Auth.Infrastructure` only for DI registration
in `Program.cs` (e.g. `AuthDbContext`, `RsaKeyProvider`). Controllers themselves have
**no dependency** on `Auth.Infrastructure` types.

## Layer responsibilities

| Layer | Responsibility |
|---|---|
| `Auth.Domain` | Value objects and pure business rules — no framework dependencies |
| `Auth.Infrastructure` | EF Core, ASP.NET Core Identity entities, repositories, JWT implementation |
| `Auth.Application` | Use-case orchestration (`AuthService`), result types — no HTTP knowledge |
| `Auth.Api` | HTTP: routing, status codes, cookies, request/response DTOs |
| `Auth.Api.Tests` | Integration tests via `WebApplicationFactory` + Testcontainers; unit tests |