
# Auth.Api Project Structure

``` text
Auth.Domain/
├── ValueObjects/       (Email, HashedPassword, etc. if you want them)
└── Rules/              (token expiry rules, password policy — plain logic, no dependencies)

Auth.Infrastructure/
├── Entities/          (User, RefreshToken -> since project uses microsoft identity package)
├── Persistence/
│   ├── AuthDbContext.cs
│   ├── Migrations/
│   └── Outbox/          (OutboxMessage entity + its EF config — no relay code, per the CDC decision)
├── Security/
│   ├── JwtTokenService.cs
│   └── Keys/             (RS256 key loading logic)
└── Auth.Infrastructure.csproj

Auth.Api/
├── Controllers/          (AuthController, JwksController)
├── Contracts/             (request/response DTOs — RegisterRequest, LoginRequest, etc.)
├── Program.cs
└── appsettings.json
```