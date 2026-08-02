# Auth.Api — Roles & Controller Separation: Implementation Guide

**Status:** Pre-development / implementation planning
**Scope:** Auth.Api only. Covers the Student/Promoter/Admin role model, self-service profile endpoints, and admin user management — structured to match the layering and patterns already established in this codebase (service layer, repository pattern, result types, outbox/CDC, unit of work).

---

## 1. Goals

- Every user is a **Student** by default (assigned at creation, never removable).
- **Promoter** and **Admin** are additional roles, layered on top.
- Self-service ("my profile") and admin ("manage all users") are **different controllers**, different route groups, different authorization requirements, different service classes — not one controller branching on role.
- Every piece of this follows patterns already in use elsewhere in Auth.Api: result types instead of exceptions for expected failures, repository interfaces instead of direct `DbContext`/`UserManager` access from services, `IUnitOfWork` for transactional boundaries, constants instead of magic strings, and business rules living in `Auth.Domain`.

---

## 2. Why separate controllers, not one controller with role checks

A single `UsersController` with `if (User.IsInRole("Admin")) { ... }` branching inside actions is the pattern to avoid here. Reasons:

- **Single Responsibility per controller.** `StudentController` only ever needs to answer "what can a user do to their own record." `AdminUsersController` only ever needs to answer "what can an admin do to any record." Mixing them means every action has two audiences to reason about, and authorization logic drifts into business logic.
- **Authorization becomes declarative, not conditional.** `[Authorize(Roles = Roles.Admin)]` on the controller class is checked by the framework before any action code runs. Branching on `User.IsInRole(...)` inside a method is checked by a human reading the method — much easier to get wrong or forget.
- **Independent evolution.** Admin tooling tends to grow differently from self-service (bulk operations, filters, exports). Keeping them separate means `AdminUsersController` can grow without ever touching `StudentController`'s contract, and vice versa.
- **This mirrors a decision already made in the architecture planning doc** — Analytics.Api's `/admin/*`, `/me/*`, `/public/*` split. Auth.Api should follow the same convention for consistency across the system: audience-segmented route groups, not one controller per entity.

---

## 3. Role model

### 3.1 Role constants — `Auth.Domain/Rules/Roles.cs`

Business rule, zero framework dependency → belongs in `Auth.Domain`, same reasoning as `AuthLifetimes` and `EmailMasking`.

```csharp
namespace Auth.Domain.Rules;

public static class Roles
{
    public const string Student = "Student";
    public const string Promoter = "Promoter";
    public const string Admin = "Admin";

    public static readonly string[] All = [Student, Promoter, Admin];

    /// <summary>
    /// Roles that can never be removed from a user once assigned.
    /// Enforced in AdminUserService, not just documented here.
    /// </summary>
    public static readonly string[] Protected = [Student];
}
```

### 3.2 Role seeding — `Auth.Infrastructure/Identity/RoleSeeder.cs`

Roles must exist as `AspNetRoles` rows before they can be assigned. Seed on startup, idempotently:

```csharp
namespace Auth.Infrastructure.Identity;

using Auth.Domain.Rules;
using Microsoft.AspNetCore.Identity;

public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}
```

Called once in `Program.cs`, after `app.Build()`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await RoleSeeder.SeedAsync(roleManager);
}
```

### 3.3 Assigning `Student` at every account-creation path

There are currently **two** places a `User` row is created — this list must be kept in sync as new creation paths are added:

- `AuthService.RegisterAsync` (email/code verification flow)
- `OAuthService.CreateAndLinkNewUserAsync` (Google sign-in, new user)

Both call, immediately after `CreateAsync`/`CreateWithoutPasswordAsync` succeeds:

```csharp
await users.AddToRoleAsync(user, Roles.Student);
```

**Maintainability note:** this duplication is a known, accepted seam for now. If a third creation path is added, consider extracting a shared `Auth.Application` helper (e.g. `IUserProvisioningService.ProvisionNewUserAsync`) that both `AuthService` and `OAuthService` call through, so role assignment (and any future "what happens on account creation" logic — e.g. publishing `user-registered`) lives in exactly one place. Not worth doing for two call sites; worth doing at three.

### 3.4 Roles in the JWT

`ClaimTypes.Role` is the claim type ASP.NET Core's `[Authorize(Roles = ...)]` reads by convention — using anything else silently breaks role-based authorization.

`ITokenService`:
```csharp
string GenerateAccessToken(User user, IEnumerable<string> roles);
```

Every token-issuance site (`RegisterAsync`, `LoginAsync`, `RefreshAsync`, `OAuthService.IssueTokensAsync`, `ConfirmLinkAsync`) must fetch roles before generating the token:

```csharp
var roles = await users.GetRolesAsync(user);
var accessToken = tokens.GenerateAccessToken(user, roles);
```

Since roles are baked into the token at issuance, a role change (e.g. promotion to Admin) only takes effect once the user's **next access token** is issued (on next login or refresh) — same staleness tradeoff already accepted for the `isPremium` claim. This is fine for Promoter/Admin (not security-critical to propagate in real time), but worth stating explicitly rather than assuming.

---

## 4. New profile fields

Self-service profile editing needs fields beyond what `User : IdentityUser<Guid>` already provides. Add to `Auth.Infrastructure/Identity/User.cs`:

```csharp
public class User : IdentityUser<Guid>
{
    public bool IsPremium { get; set; }
    public DateTime? PremiumUntil { get; set; }
    public DateTime CreatedAt { get; set; }

    // new — self-service editable profile fields
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    // Note: IdentityUser<Guid> already provides PhoneNumber and PhoneNumberConfirmed —
    // reuse those rather than adding a duplicate field.
}
```

```bash
dotnet ef migrations add AddProfileFields --project Auth.Infrastructure --startup-project Auth.Api
```

---

## 5. Controller & route layout

```
/api/me/*        → StudentController        [Authorize] (any authenticated user — every user is at least a Student)
/api/admin/*      → AdminUsersController      [Authorize(Roles = Roles.Admin)]
/api/promoter/*   → (future) PromoterController  [Authorize(Roles = Roles.Promoter)]
```

`/api/me/*` is intentionally **not** `[Authorize(Roles = Roles.Student)]`. Since every account has the Student role, `[Authorize]` alone (any authenticated user) and `[Authorize(Roles = Roles.Student)]` are currently equivalent — but `[Authorize]` is the more honest statement of intent: "any signed-in user," not "specifically a Student as opposed to a Promoter/Admin" (a Promoter is *also* a Student and should still be able to view/edit their own profile).

### 5.1 `StudentController` — self-service

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/me` | Return the caller's own profile |
| `PATCH` | `/api/me` | Update `FirstName`, `LastName`, `PhoneNumber` |
| `POST` | `/api/me/change-password` | Change password (requires current password) |
| `DELETE` | `/api/me` | Self-deactivate (soft delete, per earlier design discussion) |

### 5.2 `AdminUsersController` — admin management

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/admin/users` | Paginated/searchable user list |
| `GET` | `/api/admin/users/{userId}` | Single user detail |
| `POST` | `/api/admin/users/{userId}/roles/{role}` | Assign a role |
| `DELETE` | `/api/admin/users/{userId}/roles/{role}` | Remove a role (blocked for `Roles.Protected`) |
| `POST` | `/api/admin/users/{userId}/deactivate` | Force-deactivate an account |

---

## 6. Layering: where each piece lives

Following the established pattern (`AuthController` → `IAuthService`/`AuthService` → repositories → `AuthDbContext`), both new feature areas get their own service pair:

```
Auth.Api/
├── Controllers/
│   ├── StudentController.cs
│   └── AdminUsersController.cs
└── Contracts/
    ├── UpdateProfileRequest.cs
    ├── ChangePasswordRequest.cs
    └── AssignRoleRequest.cs (if role isn't purely a route param)

Auth.Application/
├── Results/
│   ├── ProfileResults.cs
│   └── AdminUserResults.cs
└── Services/
    ├── IStudentProfileService.cs / StudentProfileService.cs
    └── IAdminUserService.cs / AdminUserService.cs

Auth.Domain/
└── Rules/
    └── Roles.cs

Auth.Infrastructure/
├── Identity/
│   ├── User.cs (extended)
│   └── RoleSeeder.cs
└── Repositories/
    └── IUserRepository.cs (extended — see §7)
```

**Why two services instead of one `UserService` used by both controllers:** `StudentProfileService` and `AdminUserService` operate under different trust assumptions — the former always scopes to "the currently authenticated user's own id," the latter takes an arbitrary `userId` from an admin caller. Merging them into one service means every method needs to either accept a "is this the admin path or the self path" flag, or trust the caller to pass the right id — both are worse than two small, single-purpose services that can't be confused for each other.

---

## 7. `IUserRepository` additions

```csharp
// Self-service
Task<User?> FindByIdAsync(Guid id);                                 // likely already exists
Task<IdentityResult> UpdateProfileAsync(User user, string? firstName, string? lastName, string? phoneNumber);
Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword);
Task<IdentityResult> DeactivateAsync(User user);

// Admin
Task<(IReadOnlyList<User> Users, int TotalCount)> SearchUsersAsync(string? searchTerm, int page, int pageSize);
Task AddToRoleAsync(User user, string role);
Task RemoveFromRoleAsync(User user, string role);
Task<IList<string>> GetRolesAsync(User user);
```

Implementations wrap `UserManager<User>` the same way every other repository method in this codebase already does — no new pattern introduced here.

`DeactivateAsync` — implement as a soft-delete, consistent with the earlier design discussion on user deletion:
```csharp
public async Task<IdentityResult> DeactivateAsync(User user)
{
    user.LockoutEnabled = true;
    user.LockoutEnd = DateTimeOffset.MaxValue; // effectively permanent, via Identity's own lockout mechanism
    return await userManager.UpdateAsync(user);
}
```
Reusing Identity's built-in lockout fields for deactivation avoids adding a redundant `IsDeactivated` column — `LockoutEnd = MaxValue` already blocks `CheckPasswordSignInAsync`-based flows, and your `Login`'s existing `IsLockedOutAsync` check (added during the OAuth link-attempt work) already respects it.

---

## 8. Result types

Same shape as every other result type in this codebase — enum status + factory methods, no exceptions for expected outcomes.

```csharp
// Auth.Application/Results/ProfileResults.cs
namespace Auth.Application.Results;

public enum UpdateProfileStatus { Success, ValidationFailed }

public class UpdateProfileResult
{
    public UpdateProfileStatus Status { get; private init; }
    public IEnumerable<string> Errors { get; private init; } = [];

    public static UpdateProfileResult Success() => new() { Status = UpdateProfileStatus.Success };
    public static UpdateProfileResult ValidationFailed(IEnumerable<string> errors) =>
        new() { Status = UpdateProfileStatus.ValidationFailed, Errors = errors };
}

public enum ChangePasswordStatus { Success, IncorrectCurrentPassword, ValidationFailed }

public class ChangePasswordResult
{
    public ChangePasswordStatus Status { get; private init; }
    public IEnumerable<string> Errors { get; private init; } = [];

    public static ChangePasswordResult Success() => new() { Status = ChangePasswordStatus.Success };
    public static ChangePasswordResult IncorrectCurrentPassword() => new() { Status = ChangePasswordStatus.IncorrectCurrentPassword };
    public static ChangePasswordResult ValidationFailed(IEnumerable<string> errors) =>
        new() { Status = ChangePasswordStatus.ValidationFailed, Errors = errors };
}
```

```csharp
// Auth.Application/Results/AdminUserResults.cs
namespace Auth.Application.Results;

public enum AssignRoleStatus { Success, UserNotFound, UnknownRole }
public enum RemoveRoleStatus { Success, UserNotFound, UnknownRole, RoleIsProtected, LastAdminCannotBeRemoved }

public class AssignRoleResult
{
    public AssignRoleStatus Status { get; private init; }
    public static AssignRoleResult Success() => new() { Status = AssignRoleStatus.Success };
    public static AssignRoleResult UserNotFound() => new() { Status = AssignRoleStatus.UserNotFound };
    public static AssignRoleResult UnknownRole() => new() { Status = AssignRoleStatus.UnknownRole };
}

public class RemoveRoleResult
{
    public RemoveRoleStatus Status { get; private init; }
    public static RemoveRoleResult Success() => new() { Status = RemoveRoleStatus.Success };
    public static RemoveRoleResult UserNotFound() => new() { Status = RemoveRoleStatus.UserNotFound };
    public static RemoveRoleResult UnknownRole() => new() { Status = RemoveRoleStatus.UnknownRole };
    public static RemoveRoleResult RoleIsProtected() => new() { Status = RemoveRoleStatus.RoleIsProtected };
    public static RemoveRoleResult LastAdminCannotBeRemoved() => new() { Status = RemoveRoleStatus.LastAdminCannotBeRemoved };
}
```

---

## 9. `StudentProfileService`

```csharp
namespace Auth.Application.Services;

using Auth.Application.Results;
using Auth.Infrastructure.Repositories;

public interface IStudentProfileService
{
    Task<User?> GetProfileAsync(Guid userId);
    Task<UpdateProfileResult> UpdateProfileAsync(Guid userId, string? firstName, string? lastName, string? phoneNumber);
    Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task DeactivateAsync(Guid userId);
}

public class StudentProfileService(IUserRepository users, IRefreshTokenRepository refreshTokens, IUnitOfWork unitOfWork)
    : IStudentProfileService
{
    public async Task<User?> GetProfileAsync(Guid userId) => await users.FindByIdAsync(userId);

    public async Task<UpdateProfileResult> UpdateProfileAsync(Guid userId, string? firstName, string? lastName, string? phoneNumber)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return UpdateProfileResult.ValidationFailed(["User not found."]);

        var result = await users.UpdateProfileAsync(user, firstName, lastName, phoneNumber);
        return result.Succeeded
            ? UpdateProfileResult.Success()
            : UpdateProfileResult.ValidationFailed(result.Errors.Select(e => e.Description));
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return ChangePasswordResult.IncorrectCurrentPassword(); // don't leak existence

        var result = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            // Password change should kill existing sessions, same policy as password reset.
            await refreshTokens.RevokeAllForUserAsync(userId);
            await unitOfWork.SaveChangesAsync();
            return ChangePasswordResult.Success();
        }

        var isWrongCurrentPassword = result.Errors.Any(e => e.Code == "PasswordMismatch");
        return isWrongCurrentPassword
            ? ChangePasswordResult.IncorrectCurrentPassword()
            : ChangePasswordResult.ValidationFailed(result.Errors.Select(e => e.Description));
    }

    public async Task DeactivateAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return;

        await users.DeactivateAsync(user);
        await refreshTokens.RevokeAllForUserAsync(userId); // kill all sessions on deactivation
        await unitOfWork.SaveChangesAsync();
    }
}
```

---

## 10. `AdminUserService`

The two guardrails from the earlier design discussion — never remove `Student`, never remove the last `Admin` — are enforced **here**, in the service layer, not just documented as a convention. This is the layer responsible for protecting that invariant regardless of which controller (or future caller) invokes it.

```csharp
namespace Auth.Application.Services;

using Auth.Application.Results;
using Auth.Domain.Rules;
using Auth.Infrastructure.Repositories;

public interface IAdminUserService
{
    Task<(IReadOnlyList<User> Users, int TotalCount)> SearchUsersAsync(string? searchTerm, int page, int pageSize);
    Task<User?> GetUserAsync(Guid userId);
    Task<AssignRoleResult> AssignRoleAsync(Guid userId, string role);
    Task<RemoveRoleResult> RemoveRoleAsync(Guid userId, string role);
    Task DeactivateUserAsync(Guid userId);
}

public class AdminUserService(IUserRepository users, IUnitOfWork unitOfWork) : IAdminUserService
{
    public Task<(IReadOnlyList<User> Users, int TotalCount)> SearchUsersAsync(string? searchTerm, int page, int pageSize) =>
        users.SearchUsersAsync(searchTerm, page, pageSize);

    public Task<User?> GetUserAsync(Guid userId) => users.FindByIdAsync(userId);

    public async Task<AssignRoleResult> AssignRoleAsync(Guid userId, string role)
    {
        if (!Roles.All.Contains(role))
            return AssignRoleResult.UnknownRole();

        var user = await users.FindByIdAsync(userId);
        if (user is null) return AssignRoleResult.UserNotFound();

        await users.AddToRoleAsync(user, role);
        await unitOfWork.SaveChangesAsync();
        return AssignRoleResult.Success();
    }

    public async Task<RemoveRoleResult> RemoveRoleAsync(Guid userId, string role)
    {
        if (!Roles.All.Contains(role))
            return RemoveRoleResult.UnknownRole();

        if (Roles.Protected.Contains(role))
            return RemoveRoleResult.RoleIsProtected();

        var user = await users.FindByIdAsync(userId);
        if (user is null) return RemoveRoleResult.UserNotFound();

        if (role == Roles.Admin && await users.CountUsersInRoleAsync(Roles.Admin) <= 1)
            return RemoveRoleResult.LastAdminCannotBeRemoved();

        await users.RemoveFromRoleAsync(user, role);
        await unitOfWork.SaveChangesAsync();
        return RemoveRoleResult.Success();
    }

    public async Task DeactivateUserAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return;

        await users.DeactivateAsync(user);
        await unitOfWork.SaveChangesAsync();
    }
}
```

`CountUsersInRoleAsync` is one more small addition to `IUserRepository`:
```csharp
Task<int> CountUsersInRoleAsync(string role); // wraps userManager.GetUsersInRoleAsync(role).Count, or a direct query
```

---

## 11. Controllers

### `StudentController`

```csharp
namespace Auth.Api.Controllers;

using System.Security.Claims;
using Auth.Api.Contracts;
using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/me")]
[Authorize] // any authenticated user — every account is at minimum a Student
public class StudentController(IStudentProfileService profileService) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await profileService.GetProfileAsync(CurrentUserId);
        if (user is null) return NotFound();

        return Ok(new ProfileResponse(user.Id, user.Email!, user.FirstName, user.LastName, user.PhoneNumber));
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest req)
    {
        var result = await profileService.UpdateProfileAsync(CurrentUserId, req.FirstName, req.LastName, req.PhoneNumber);

        return result.Status switch
        {
            UpdateProfileStatus.Success => NoContent(),
            UpdateProfileStatus.ValidationFailed => BadRequest(new { errors = result.Errors }),
            _ => throw new InvalidOperationException($"Unhandled status: {result.Status}")
        };
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var result = await profileService.ChangePasswordAsync(CurrentUserId, req.CurrentPassword, req.NewPassword);

        return result.Status switch
        {
            ChangePasswordStatus.Success => NoContent(),
            ChangePasswordStatus.IncorrectCurrentPassword => BadRequest(new { message = "Current password is incorrect." }),
            ChangePasswordStatus.ValidationFailed => BadRequest(new { errors = result.Errors }),
            _ => throw new InvalidOperationException($"Unhandled status: {result.Status}")
        };
    }

    [HttpDelete]
    public async Task<IActionResult> Deactivate()
    {
        await profileService.DeactivateAsync(CurrentUserId);
        return NoContent();
    }
}
```

**Note on `CurrentUserId`:** it reads from the JWT's own claims (`sub`), never from a route parameter or request body. This is the enforcement point for "self-service only operates on the caller's own record" — there is no way for this controller to act on any other user's data, by construction, not by convention.

### `AdminUsersController`

```csharp
namespace Auth.Api.Controllers;

using Auth.Api.Contracts;
using Auth.Application.Results;
using Auth.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (users, total) = await adminUserService.SearchUsersAsync(q, page, pageSize);
        return Ok(new { users, total, page, pageSize });
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId)
    {
        var user = await adminUserService.GetUserAsync(userId);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("{userId:guid}/roles/{role}")]
    public async Task<IActionResult> AssignRole(Guid userId, string role)
    {
        var result = await adminUserService.AssignRoleAsync(userId, role);
        return result.Status switch
        {
            AssignRoleStatus.Success => NoContent(),
            AssignRoleStatus.UserNotFound => NotFound(),
            AssignRoleStatus.UnknownRole => BadRequest(new { message = "Unknown role." }),
            _ => throw new InvalidOperationException($"Unhandled status: {result.Status}")
        };
    }

    [HttpDelete("{userId:guid}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(Guid userId, string role)
    {
        var result = await adminUserService.RemoveRoleAsync(userId, role);
        return result.Status switch
        {
            RemoveRoleStatus.Success => NoContent(),
            RemoveRoleStatus.UserNotFound => NotFound(),
            RemoveRoleStatus.UnknownRole => BadRequest(new { message = "Unknown role." }),
            RemoveRoleStatus.RoleIsProtected => BadRequest(new { message = "This role cannot be removed." }),
            RemoveRoleStatus.LastAdminCannotBeRemoved => Conflict(new { message = "Cannot remove the last remaining admin." }),
            _ => throw new InvalidOperationException($"Unhandled status: {result.Status}")
        };
    }

    [HttpPost("{userId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid userId)
    {
        await adminUserService.DeactivateUserAsync(userId);
        return NoContent();
    }
}
```

Every switch ends in a `throw` on the `default`/unhandled case — consistent with the pattern already established, and the earlier lesson from this conversation still applies: **whenever a result enum gains a new case, grep the controller's switch and add it in the same commit**, since an unhandled case here throws a 500 rather than failing to compile.

---

## 12. `Program.cs` registrations

```csharp
builder.Services.AddScoped<IStudentProfileService, StudentProfileService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();

// after app.Build():
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await RoleSeeder.SeedAsync(roleManager);
}
```

---

## 13. Contracts

```csharp
// Auth.Api/Contracts/ProfileResponse.cs
public record ProfileResponse(Guid Id, string Email, string? FirstName, string? LastName, string? PhoneNumber);

// Auth.Api/Contracts/UpdateProfileRequest.cs
public record UpdateProfileRequest(string? FirstName, string? LastName, string? PhoneNumber);

// Auth.Api/Contracts/ChangePasswordRequest.cs
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
```

---

## 14. Testing checklist (Testcontainers pattern, as established)

| Test | Why it matters |
|---|---|
| `Register_NewUser_IsAssignedStudentRole` | Confirms §3.3 didn't get skipped on the email/code path |
| `GoogleLogin_NewUser_IsAssignedStudentRole` | Same, for the OAuth path — the exact kind of thing that's easy to forget on a second creation path |
| `StudentController_Get_ReturnsOnlyOwnProfile` | Confirms `CurrentUserId` is read from the token, never a route/body value |
| `AdminUsersController_WithoutAdminRole_Returns403` | Confirms `[Authorize(Roles = Roles.Admin)]` actually gates the controller |
| `RemoveRole_Student_ReturnsRoleIsProtected` | Confirms the protected-role guardrail |
| `RemoveRole_LastAdmin_ReturnsLastAdminCannotBeRemoved` | Confirms the self-lockout guardrail — worth seeding exactly one Admin in this test and asserting the removal is rejected |
| `AccessToken_ContainsRoleClaims_AsClaimTypeRole` | Confirms roles are wired using `ClaimTypes.Role`, not a custom claim name — a `[Authorize(Roles=...)]` test failing silently due to the wrong claim type is a hard bug to spot without a direct assertion on this |

---

## 15. Summary — patterns applied, nothing new introduced

Every piece of this feature reuses a pattern already present elsewhere in Auth.Api:

| Pattern | Where else it's used | Where it's used here |
|---|---|---|
| Result types instead of exceptions | `RegisterResult`, `LoginResult`, `ConfirmLinkResult` | `UpdateProfileResult`, `AssignRoleResult`, `RemoveRoleResult` |
| Repository interfaces, no direct `UserManager` in services | `IUserRepository` throughout | Extended, not replaced |
| `IUnitOfWork` for transactional boundaries | `AuthService.RegisterAsync` | `AdminUserService`, `StudentProfileService` |
| Business constants in `Auth.Domain` | `AuthLifetimes`, `EmailMasking` | `Roles` |
| Controller = thin HTTP adapter, switch on result status | `AuthController`, `OAuthController` | `StudentController`, `AdminUsersController` |
| Soft-delete via Identity's own lockout fields | (new here) | `DeactivateAsync` reuses `LockoutEnd` rather than adding a new column |
| Audience-segmented route groups | Analytics.Api's `/admin/*`/`/me/*`/`/public/*` (planning doc) | Auth.Api's `/api/me/*`/`/api/admin/*` |

No new architectural concept is introduced by this feature — it's an application of everything already decided in this codebase to a new area (user roles and profile management), which is the intended payoff of having established these patterns early.