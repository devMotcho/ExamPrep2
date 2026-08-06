# ADR 005: Placing Identity Entities in the Infrastructure Layer

## Status
Accepted

## Context
In strict Clean Architecture, all core entities (like `User`, `Role`) should live in the **Domain Layer**. The Domain Layer is supposed to be "pure" — meaning it should have zero dependencies on external frameworks, databases, or ORMs (like Entity Framework).

However, in this project, we rely heavily on **ASP.NET Core Identity** for secure password hashing, lockout logic, and JWT integration. To use this, our database user class must inherit from `IdentityUser`. 

If we put a class that inherits from `IdentityUser` into the Domain Layer, we would be forcing the pure Domain Layer to depend on Microsoft's Identity and Entity Framework packages, violating the core rule of Clean Architecture.

## Decision
I decided to keep the Entity Framework / Identity specific classes (e.g., `User`, `RefreshToken`, `EmailVerificationCode`) inside the **Infrastructure Layer** (`Auth.Infrastructure.Identity`).

To keep the inner layers pure:
1. The **Application Layer** defines its own pure domain models (e.g., `AppUser`).
2. The **Infrastructure Layer** fetches the `User` from the database, maps it to the pure `AppUser` model, and returns it to the Application Layer.

## Consequences
* **Pros:** 
  * The Domain and Application layers remain 100% pure and unaware of Entity Framework or ASP.NET Core Identity.
  * We can easily swap out ASP.NET Identity in the future without changing a single line of business logic.
* **Cons:** 
  * We have to write mapping code to translate back and forth between the Infrastructure's `User` and the Application's `AppUser`.

## Diagram
```mermaid
flowchart TD
    subgraph Application Layer [Application Layer (Pure)]
        AppUser[AppUser Model]
        IUserRepo[IUserRepository Interface]
        AuthService[AuthService]
        AuthService --> |Calls| IUserRepo
        IUserRepo --> |Returns| AppUser
    end

    subgraph Infrastructure Layer [Infrastructure Layer (Dirty)]
        User[User : IdentityUser]
        UserRepo[UserRepository]
        UserRepo -.-> |Implements| IUserRepo
        UserRepo --> |Queries DB for| User
        UserRepo --> |Maps to| AppUser
    end
```
