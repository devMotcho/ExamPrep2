# EscolhaMúltipla.pt (v2) - Exam Preparation Platform

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192)
![Docker](https://img.shields.io/badge/Docker-2496ED)
![Kafka](https://img.shields.io/badge/Kafka-231F20)

Welcome to Version 2 of the Escolha Múltipla platform!

## The "Why" Behind Version 2

This project is the complete, professional rewrite of my personal platform currently running in production at [escolhamultipla.pt](https://escolhamultipla.pt). 

Version 1 (the current production app) was an MVP (Minimum Viable Product). I built it rapidly in a single weekend using "vibe code" just so my friends and I could study for our upcoming university exams. While it works, it wasn't built following software development best practices, scalable architecture, or testing standards.

Version 2 (this repository) was born out of a desire to do it right. I decided to rewrite the entire platform from scratch to demonstrate my capabilities as a software engineer. This version strictly adheres to:
* Microservices Architecture (event-driven with Kafka)
* Clean Architecture and SOLID Principles
* Domain-Driven Design (DDD) concepts
* Extensive Automated Testing (Integration and Unit tests via xUnit & Testcontainers)
* Scalable Infrastructure designed for enterprise loads

---

## Technologies & Tools

I built this platform utilizing a modern, scalable tech stack, heavily tailored around my personal development workflow:

* Development Environment: Ghostty, Neovim (fully customized for C# / .NET development)
* API Testing & Documentation: Postman & Swagger (OpenAPI)
* Backend Framework: .NET 9 (C#) using ASP.NET Core Web API
* Database & ORM: PostgreSQL & Entity Framework Core (EF Core)
* Caching & Stateful Security: Redis (Distributed caching & JWT Blocklists)
* Identity & Security: ASP.NET Core Identity, JWT Bearer Auth (JWKS), Custom OTP generation
* Message Broker: Apache Kafka (for asynchronous microservice communication)
* Containerization: Docker & Docker Compose
* Testing: xUnit, Moq, Testcontainers (for real database integration testing)

---

## About the Platform

EscolhaMúltipla is a collaborative multiple-choice exam study tool built for students who take results seriously. The platform takes students from zero to exam-ready by allowing them to:

1. Create Study Areas: Dedicated public or private areas for specific subjects.
2. Build Question Banks: Import questions in bulk via JSON or use a rich text editor.
3. Configure Practice Sessions: Set wrong-answer penalties, instant feedback, and countdown timers.
4. Take the Exam: Practice under real pressure with a live countdown clock.
5. Get Graded (0–20 Scale): Receive an instant academic grade, review per-question answers, and read explanations.
6. Track History: Keep a persistent dashboard of score evolutions and identify weak chapters.

*The platform offers a Free Standard Tier (limits on uploads and daily exams) and a Premium Tier for power users.*

---

## Currently Implemented Features

As this is an ongoing rewrite using a Microservices architecture, the current focus has been perfecting the Authentication & Identity Service (`Auth.Api`).

### Auth Service (`Auth.Api`)
- Robust RBAC System: Full Role-Based Access Control (Student, Promoter, Partner, Admin, SuperAdmin).
- Partner System: Advanced referral system allowing user linking at registration, integrated transactional ledgers for revenue sharing (balances and manual payouts), and Outbox Pattern notifications.
- Secure Registration & Login: JWT generation signed with Asymmetric RSA keys (JWKS) and embedded role claims.
- Stateless Logout & Token Revocation: Redis-backed JWT blocklist instantly revokes access tokens system-wide without losing stateless authentication benefits.
- Email Verification (OTP): Custom, database-backed OTP generation and verification for confirming new user accounts.
- Profile Management: Self-service profile updates (names, phone numbers), secure password changes, and account deactivation.
- Admin Dashboard APIs: Optimized endpoints (`ILike` PostgreSQL text searches, batched EF Core queries) to list, manage, and assign roles to users.
- 100% Test Coverage: Verified by dozens of robust integration tests using real PostgreSQL Testcontainers.

---

## Roadmap / TODOs

The following features represent the core domain of the application and are scheduled for development in upcoming microservices (e.g., `Study.Api`, `Exams.Api`):

- [ ] Study Areas: Creation of public/private subjects and sharing mechanisms.
- [ ] Question Banks: CRUD for multiple-choice questions, rich-text support, and bulk JSON imports.
- [ ] Exam Engine: Real-time exam sessions, answer tracking, and auto-submit mechanisms.
- [ ] Grading Engine: 0-20 academic grading algorithms with configurable penalty factors.
- [ ] History & Dashboard: Session history tracking and score evolution analytics.
- [ ] Premium Subscription Logic: Enforcing daily limits (20 questions, 2 exams) for Free users and enabling advanced timers/chapter grouping for Premium users.
- [ ] S3 Integration: Cloud media gallery support for images inside questions.

---

## Getting Started

### 1. Configure Secrets (.env)
All sensitive configurations (Database strings, Google OAuth secrets, SMTP passwords) have been extracted from `appsettings.json` and moved into a secure `.env` file for Docker.
To set up your local secrets:
1. Navigate to the `infra/` folder.
2. Copy the example file: `cp .env.example .env`
3. Open the new `.env` file and fill in your actual secrets (e.g., Google App Password for emails, OAuth Client ID).
*(Note: `*.env` is ignored by Git to prevent accidental credential leaks).*

### 2. Run the Infrastructure
To run the local infrastructure (PostgreSQL, Kafka, Notification Worker, etc.):

```bash
cd infra
docker compose up --build -d
```

Helpful Commands:
If you want to monitor the Kafka message bus for newly registered users, you can run:
```bash
docker exec examprep-kafka kafka-console-consumer --bootstrap-server localhost:9092 --topic user-registered --from-beginning --max-messages 1
```

---
*© 2026 escolhamultipla.pt. Developed by B.Mamede*
