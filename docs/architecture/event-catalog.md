# ExamPrep — Event Catalog

Registry of all Kafka topics used across services. Full schemas live in `contracts/events/<topic>.<version>.md`.

Delivery semantics: all events are delivered **at-least-once** via Debezium CDC reading each service's `outbox_messages` table. Consumers must be idempotent (dedupe on the event payload's entity id).

| Topic | Status | Producer | Consumers | Schema |
|---|---|---|---|---|
| `user-registered` | ✅ Implemented | Auth.Api | Exams.Api, Analytics.Api | [v1](../../contracts/events/user-registered.v1.md) |
| `password-reset-requested` | 🔜 Planned | Auth.Api | Notification.Api | - |
| `email-verification-requested` | 🔜 Planned | Auth.Api | Notification.Api | - |
| `user-upgraded-premium` | 🔜 Planned | Auth.Api | Exams.Api, Analytics.Api | - |
| `payment-completed` | 🔜 Planned | Payments.Api | Auth.Api | - |
| `exam-session-completed` | 🔜 Planned | Exams.Api | Analytics.Api | - |
| `study-area-deleted` | 🔜 Planned | Exams.Api | Analytics.Api | — |
| `question-imported` | 🔜 Planned | Exams.Api | Analytics.Api | — |

## Conventions

- **Topic naming**: `kebab-case`, past-tense verb (`user-registered`, not `register-user`) — describes something that already happened, not a command.
- **Key**: always the primary entity's id (e.g. `user.Id` for `user-registered`), so Kafka partitions by entity and preserves per-entity ordering.
- **Versioning**: breaking changes get a new topic suffix (`user-registered-v2`) rather than mutating `v1`'s shape in place. Additive, optional fields don't require a version bump.
- **Adding a new event**: add a row here, create `contracts/events/<topic>.v1.md` with the field list and an example payload, then implement the outbox write in the producing service.