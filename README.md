# Payment Microservices

A distributed payment processing system built with .NET 10, featuring fund reservation, authorization, and settlement.

## Architecture

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   Payment   │──────▶│   Ledger   │     │  Settlement │
│   Service   │      │   Service   │      │   Worker    │
│   (Mongo)   │◀─────│  (Postgres) │◀────│             │
└──────┬──────┘      └─────────────┘      └─────────────┘
       │
       ▼
   RabbitMQ (Events)
```

## Services

| Service | Port | Database | Description |
|---------|------|----------|-------------|
| Payment | 5002 | MongoDB | Authorizes payments, stores payment intents |
| Ledger | 5001 | PostgreSQL | Manages fund reservations (reserve/release/settle) |
| Settlement | - | - | Background worker for bank settlement |

## Quick Start

```bash
cd Demo.Infra
docker-compose up
```

Services start at:
- Payment API: http://localhost:5002
- Ledger API: http://localhost:5001
- RabbitMQ Management: http://localhost:15677 (guest/guest)

## API Endpoints

### Payment Service
```
POST /authorize          # Authorize a payment (idempotent)
```

### Ledger Service
```
POST /ledger/reserve     # Reserve funds
POST /ledger/release     # Release reservation
POST /ledger/settle      # Settle reservation
```

## Flow

1. **Authorize**: Payment service creates intent → reserves funds in Ledger → publishes `PaymentAuthorized`
2. **Settlement**: Worker consumes event → calls bank simulator → settles or releases funds
3. **Idempotency**: All authorization requests are idempotent via `Idempotency-Key` header

## Stored Procedures (PostgreSQL)

Located in `Demo.Ledger/src/Demo.Ledger.Service/Scripts/`:

| Procedure | Purpose |
|-----------|---------|
| `process_reservation()` | Atomically checks balance, creates reservation, updates held funds |
| `release_reservation()` | Releases held funds (marks as REVERSED) |
| `settle_reservation()` | Deducts from balance and held_balance, marks SETTLED |

All procedures use `FOR UPDATE` row locking for consistency.

## Tech Stack

- .NET 10
- MongoDB (payment storage)
- PostgreSQL (ledger with stored procedures)
- RabbitMQ (async messaging)
- Redis (caching)
- Docker Compose

## Project Structure

```
Demo.Payment/       # Payment authorization service
Demo.Ledger/        # Fund reservation service
Demo.Settlement/    # Background settlement worker
Demo.Common/        # Shared contracts & utilities
Demo.Infra/         # Docker Compose & K8s manifests
packages/           # Local NuGet packages
```

## Tests

Integration tests in `Demo.Ledger.Tests/` using:
- **xUnit** + **FluentAssertions**
- **Testcontainers** (PostgreSQL 15 in Docker)
- **DbUp** (runs migrations before tests)

```bash
cd Demo.Ledger/tests/Demo.Ledger.Tests
dotnet test
```

Tests cover:
- Reservation with sufficient funds
- Rejection on insufficient funds
- Database migration validation
