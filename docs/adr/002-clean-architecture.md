# ADR 002: Adopt Clean Architecture and SOLID Principles

## Status
Accepted

## Context
In the first version of the platform, database queries (EF Core), business logic, and API endpoints were all mixed together in the Controllers. This made it impossible to unit test the business rules without a real database, and changing a database column often broke the API response. I needed a better way to structure the code.

## Decision
I decided to adopt **Clean Architecture** for all microservices in the V2 rewrite. The solution is divided into four distinct layers:

1. **Domain:** The core business rules and entities (e.g., `User` models, `AuthLifetimes`). No external dependencies.
2. **Application:** The use cases (e.g., `AuthService`). It defines interfaces for repositories but doesn't implement them.
3. **Infrastructure:** The actual database implementations (EF Core `AuthDbContext`), Email senders, and JWT generation.
4. **Api:** The Controllers and presentation logic. It only depends on the Application layer.

## Consequences
* **Pros:** 
  * Very easy to test. I can mock the `IUserRepository` to test the `AuthService` logic (Unit Testing).
  * Highly maintainable. Swapping out PostgreSQL for another DB only requires changing the Infrastructure layer.
* **Cons:** 
  * Lots of boilerplate code. A simple feature requires touching 3-4 different files (DTOs, Interfaces, Services, Controllers).

## Diagram
```mermaid
architecture-beta
    group api(Api Layer)
    group app(Application Layer)
    group infra(Infrastructure Layer)
    group dom(Domain Layer)

    service controller(Controllers) in api
    service service(Services) in app
    service repo(Repositories) in infra
    service entity(Entities) in dom

    controller --> service
    repo --> entity
    service --> entity
    infra --> app
```
*(Simplified dependency flow: Inner layers never depend on outer layers)*
