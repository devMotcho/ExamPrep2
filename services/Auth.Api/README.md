# Auth.Api Service

## Project Structure

```text
Auth.Api.sln
├── Auth.Domain/               ← zero external dependencies
│   ├── Entities/              (domain entities — no EF/Identity coupling)
│   ├── ValueObjects/
│   └── Rules/                 (pure business rules: password policy, token expiry)
│
├── Auth.Application/          ← depends on Auth.Domain only
│   ├── Events/
│   │   └── UserRegisteredEvent.cs
│   ├── Interfaces/            (all port interfaces defined here)
│   │   ├── IUserRepository.cs
│   │   ├── IRefreshTokenRepository.cs
│   │   ├── IOutboxRepository.cs
│   │   ├── IUnitOfWork.cs
│   │   ├── ITransaction.cs
│   │   ├── ITokenService.cs
│   │   └── IJwksProvider.cs
│   ├── Models/
│   │   ├── AppUser.cs
│   │   └── CreateUserResult.cs
│   ├── Results/
│   │   └── RegisterResult.cs
│   └── Services/
│       ├── IAuthService.cs
│       └── AuthService.cs     (register use case: transaction, repos, outbox, tokens)
│
├── Auth.Infrastructure/       ← depends on Auth.Domain + Auth.Application
│   ├── Identity/              (User, RefreshToken — ASP.NET Core Identity entities)
│   ├── Persistence/
│   │   ├── AuthDbContext.cs
│   │   ├── UnitOfWork.cs      (implements IUnitOfWork; EfTransaction adapts IDbContextTransaction)
│   │   ├── Migrations/
│   │   └── Outbox/            (OutboxMessage entity — no relay code, CDC via Debezium)
│   ├── Repositories/          (implements IUserRepository, IRefreshTokenRepository, IOutboxRepository)
│   └── Security/
│       ├── RsaKeyProvider.cs  (implements IJwksProvider)
│       └── JwtTokenService.cs (implements ITokenService)
│
├── Auth.Api/                  ← depends on Auth.Application + Auth.Infrastructure (DI wiring only)
│   ├── Controllers/
│   │   ├── AuthController.cs  (thin HTTP adapter — maps RegisterResult → HTTP status codes)
│   │   └── JwksController.cs
│   ├── Contracts/             (RegisterRequest, AuthResponse DTOs)
│   ├── Extensions/            (PersistenceExtensions, IdentityExtensions, AuthenticationExtensions, ApplicationExtensions)
│   └── Program.cs
│
└── Auth.Api.Tests/
    ├── Fixtures/
    ├── IntegrationTests/      (RegisterEndpointTests via WebApplicationFactory + Testcontainers)
    └── UnitTests/             (EventContractTests)
```

## Dependency direction

```
Auth.Domain  ←  Auth.Application  ←  Auth.Infrastructure  ←  Auth.Api
                                                          ←  Auth.Api.Tests
```

> **Rule:** Arrows point inward. No inner layer ever references an outer layer.
> `Auth.Api` references `Auth.Infrastructure` only for DI registration in the extension
> methods — controllers themselves only inject interfaces from `Auth.Application`.

## Layer responsibilities

| Layer | Owns | Must never know about |
|---|---|---|
| `Auth.Domain` | Business entities, pure rules | EF Core, Identity, HTTP, Kafka |
| `Auth.Application` | Use-case orchestration, port interfaces, result types | EF Core, Identity, JWT libraries |
| `Auth.Infrastructure` | Persistence, Identity, JWT signing, RSA keys | HTTP, routing, controllers |
| `Auth.Api` | HTTP routing, status codes, cookies, DI wiring | Database transactions, outbox |

## Running locally

```bash
# Start dependencies (Postgres)
docker compose -f ../../infra/docker-compose.yml up -d

# Run the API (migrations run automatically on startup)
dotnet run --project Auth.Api
```

## Testing

```bash
# Integration tests use Testcontainers (no local Postgres needed)
dotnet test
```