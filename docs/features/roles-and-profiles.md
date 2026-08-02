# Roles and Profiles

This document details the architecture, design decisions, and access control models for the Role-Based Access Control (RBAC) and self-service profile management implemented in `Auth.Api`.

## 1. Overview and Architecture

The user management architecture explicitly separates self-service operations (where a user manages their own data) from administrative operations (where high-privilege users manage the system). 

### Core Roles
* **Student:** The baseline role automatically assigned to every newly registered or OAuth-linked account. This role is strictly **Protected** and can never be removed from any account.
* **Promoter:** A role for users who possess capabilities to promote or market the platform.
* **Admin:** A highly privileged role for system administrators. The system actively enforces a constraint ensuring that the last remaining Admin cannot be demoted, preventing irreversible system lockout.

### API Controllers
1. **StudentController (`/api/me`)**
   * Accessible by any authenticated user (as everyone is at minimum a Student).
   * Identifies the acting user strictly via the JWT `sub` (or `NameIdentifier`) claim to eliminate any risk of Insecure Direct Object References (IDOR).
   * Responsible for profile retrieval, profile updates (`FirstName`, `LastName`), password changes, and self-service account deactivations.

2. **AdminUsersController (`/api/admin/users`)**
   * Strictly guarded by role-based authorization: `[Authorize(Roles = Roles.Admin)]`.
   * Responsible for paginated user search, fetching specific users, assigning roles, removing roles, and forcefully deactivating accounts.

## 2. Security and Design Decisions

* **JWT Claim Baking:** User roles are mapped via `IUserRepository.GetRolesAsync` and baked directly into the JWT claims during token issuance (in `JwtTokenService`). This avoids hitting the database on every authenticated request, optimizing performance. (Note: Role changes take effect at the next token refresh cycle).
* **Safe Deactivation (Soft Delete):** Instead of physically deleting user records (which would violate referential integrity across the microservice ecosystem), "Deactivation" utilizes ASP.NET Identity's lockout mechanism. It sets `LockoutEnabled = true` and `LockoutEnd = DateTimeOffset.MaxValue`. Subsequent login attempts by deactivated users will return `HTTP 429 Too Many Requests`.
* **Immediate Session Termination:** Whenever a user changes their password or an account is deactivated (either self-service or by an Admin), the system immediately revokes all of the user's `RefreshToken` records. This ensures that any active sessions on compromised devices are forcefully terminated once the short-lived access token expires.
* **Application Layer Segregation:** The `IUserRepository` translates Entity Framework `User` models into a lightweight `AppUser` domain record. The API controllers only interact with Application Services (`StudentProfileService`, `AdminUserService`), which return strongly-typed `Result` wrappers (e.g., `ChangePasswordResult`, `AssignRoleResult`). This guarantees the controllers remain thin and devoid of business logic.

## 3. Component Sequence

1. **Self-Service Password Change**
   * **Client** requests `POST /api/me/change-password` with `CurrentPassword` and `NewPassword`.
   * **StudentController** extracts `userId` from the JWT.
   * **StudentProfileService** validates the current password via `IUserRepository`.
   * On success, the password is changed, and `IRefreshTokenRepository.RevokeAllForUserAsync(userId)` is called.
   * Returns `HTTP 204 NoContent` or `HTTP 400 Bad Request` depending on the `ChangePasswordStatus` result.

2. **Admin Role Assignment**
   * **Admin Client** requests `POST /api/admin/users/{userId}/roles/{role}`.
   * **AdminUsersController** verifies the caller's Admin JWT.
   * **AdminUserService** validates the target `role` against `Roles.All`.
   * **UserRepository** executes `UserManager.AddToRoleAsync`.
   * Returns `HTTP 204 NoContent` or `HTTP 400/404/409` depending on the constraints (e.g., removing a protected role, or removing the last admin).
