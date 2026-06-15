# Adjudicate

**Healthcare Claims Adjudication Platform built with ASP.NET Core, EF Core, SQL Server, and Testcontainers.**

---

## 1. Project Overview

**Claims adjudication** is the process by which a health insurance plan evaluates a submitted medical claim and decides whether to approve or deny it — and if approved, how much to pay. Every claim a provider submits after treating a patient goes through this process before a dollar is reimbursed.

Adjudicate is a backend API that models this workflow end-to-end: a claim is submitted for a member, run through a configurable rule engine that checks eligibility, plan coverage, and duplicate submissions, and then either approved with a calculated allowed amount or denied with a coded reason. Results are persisted to SQL Server and exposed over a clean REST API.

This project was built to demonstrate production-style backend engineering across the full vertical slice — from domain modeling and business rules through persistence, API design, and automated testing — using the kinds of patterns and technologies common in healthcare and insurance engineering.

---

## 2. Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Layer                               │
│  ClaimsController  │  ExceptionMiddleware  │  Swagger + Examples │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│                    Application Services                         │
│   IClaimSubmissionService  │  IClaimAdjudicationService         │
│   IClaimQueryService       │  Application Models (DTOs)         │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│                       Domain Layer                              │
│   Claim  │  Member  │  Plan  │  Coverage  │  AdjudicationResult │
│   AdjudicationEngine  │  Rules: Eligibility, Coverage, Duplicate│
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│                    Persistence Layer                            │
│          AdjudicateDbContext  │  EF Core Configurations         │
│          Migrations           │  Entity Type Configurations      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                    ┌────────▼────────┐
                    │   SQL Server    │
                    │    2022         │
                    └─────────────────┘
```

Each layer depends only on the layer below it. The Domain layer has zero infrastructure dependencies — all business rules are testable without a database.

---

## 3. Business Rules Implemented

### Member Eligibility
A claim is denied if the member's coverage was not active on the service date. A member is eligible when `EffectiveDate ≤ ServiceDate ≤ TerminationDate` (open-ended if no termination date exists).

### Plan Coverage
Each plan defines which service types it covers (Office Visit, Emergency, Laboratory, Radiology, Pharmacy). A claim is denied if any line item's service type is not covered under the member's plan.

### Duplicate Claim Detection
A claim is denied as a duplicate if another non-voided claim exists for the same member, on the same service date, with the same set of service codes. Matching is case-insensitive.

### Approval / Denial Workflow
Rules are evaluated in order via a chain-of-responsibility engine. The first failing rule short-circuits evaluation and returns a typed denial reason (`NotEligible`, `NotCovered`, `DuplicateClaim`). If all rules pass, the claim is approved with an allowed amount equal to the total billed amount across all claim lines. Results are persisted atomically and the claim status is updated in the same transaction.

---

## 4. Features

- **Claim submission** — submit a claim for a member with one or more service lines
- **Claim adjudication** — run the adjudication engine against a submitted claim and persist the result
- **Claim retrieval** — fetch full claim details including lines and adjudication result
- **Swagger API documentation** — interactive docs with request and response examples
- **Global exception handling** — structured `ProblemDetails` responses for 400/404/409/500
- **EF Core migrations** — versioned schema management targeting SQL Server 2022
- **SQL Server persistence** — full relational model with enums stored as strings, decimal precision configured per column, and unique constraints on business keys

---

## 5. Technology Stack

| Technology | Role |
|---|---|
| ASP.NET Core 10 | Web API framework |
| C# 13 | Primary language |
| Entity Framework Core 10 | ORM and migrations |
| SQL Server 2022 | Relational database |
| xUnit | Test framework |
| Testcontainers | Real SQL Server in Docker for integration tests |
| Docker | Container runtime for test database |
| Swashbuckle / Swagger | API documentation and examples |

---

## 6. API Endpoints

### `POST /api/claims` — Submit a Claim

**Request**
```json
{
  "memberNumber": "M-00000001",
  "serviceDate": "2024-06-15",
  "lines": [
    {
      "serviceCode": "99213",
      "serviceType": "OfficeVisit",
      "quantity": 1,
      "billedAmount": 150.00
    }
  ]
}
```

**Response `201 Created`**
```json
{
  "claimId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "claimNumber": "CLM-20240615-A1B2C3D4",
  "serviceDate": "2024-06-15",
  "status": "Submitted"
}
```

---

### `POST /api/claims/{id}/adjudicate` — Adjudicate a Claim

**Response `200 OK` (Approved)**
```json
{
  "claimId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "decision": "Approved",
  "allowedAmount": 150.00,
  "denialReason": null
}
```

**Response `200 OK` (Denied)**
```json
{
  "claimId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "decision": "Denied",
  "allowedAmount": 0.00,
  "denialReason": "NotEligible"
}
```

**Error responses:** `404 Not Found` if claim does not exist · `409 Conflict` if already adjudicated

---

### `GET /api/claims/{id}` — Get Claim Details

**Response `200 OK`**
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "claimNumber": "CLM-20240615-A1B2C3D4",
  "memberId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "serviceDate": "2024-06-15",
  "status": "Approved",
  "submittedAt": "2024-06-15T10:00:00Z",
  "lines": [
    {
      "id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "serviceCode": "99213",
      "serviceType": "OfficeVisit",
      "quantity": 1,
      "billedAmount": 150.00
    }
  ],
  "adjudication": {
    "decision": "Approved",
    "allowedAmount": 150.00,
    "denialReason": null,
    "adjudicatedAt": "2024-06-15T10:05:00Z"
  }
}
```

**Denial reason codes:** `NotEligible` · `NotCovered` · `DuplicateClaim`

---

## 7. Running Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (required for integration tests)

### Steps

```bash
# Clone the repository
git clone https://github.com/saijayanth41/Adjudicate.git
cd Adjudicate

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run all tests (Docker must be running — Testcontainers will pull SQL Server automatically)
dotnet test

# Run the API
dotnet run --project src/Adjudicate.Api
```

The API starts on `http://localhost:5000`. Swagger UI is available at `http://localhost:5000/swagger`.

> **Connection string:** the connection string is intentionally blank in `appsettings.json`. Set it via environment variable before running the API:
>
> ```bash
> export ConnectionStrings__DefaultConnection="Server=127.0.0.1,1433;Database=AdjudicateDb;User Id=sa;Password=<your_password>;Encrypt=False;"
> dotnet run --project src/Adjudicate.Api
> ```
>
> Integration tests manage their own containerized database via Testcontainers — no connection string configuration required for running tests.

---

## 8. Testing

**48 / 48 tests passing.**

Tests are organized into three groups:

### Unit Tests — `Adjudicate.Tests/Adjudication/`
Pure domain logic tests with no database or infrastructure dependency. Cover all three adjudication rules and the engine's short-circuit behavior across a range of scenarios including edge cases (service date on effective date boundary, case-insensitive duplicate matching, multi-line claim coverage).

### Integration Tests — `Adjudicate.Tests/Infrastructure/`
Test `ClaimAdjudicationService` end-to-end against a real SQL Server 2022 instance running in Docker via Testcontainers. Verify persistence of `AdjudicationResult`, claim status updates, idempotency enforcement, and the full approve/deny/duplicate paths using isolated data per test.

### API Tests — `Adjudicate.Tests/Api/`
Test the full HTTP stack using `WebApplicationFactory<Program>` against the same containerized SQL Server. Cover all three endpoints including success paths, error paths (404, 409, 400), and state transitions (submit → adjudicate → get).

The SQL Server container is shared across all integration and API tests within a test run using xUnit's `[Collection]` fixture, keeping total test time under 20 seconds.

---

## 9. Project Structure

```
Adjudicate/
├── src/
│   ├── Adjudicate.Api/
│   │   ├── Controllers/
│   │   │   └── ClaimsController.cs
│   │   ├── DTOs/
│   │   │   ├── SubmitClaimRequest.cs
│   │   │   ├── ClaimLineRequest.cs
│   │   │   ├── SubmitClaimResponse.cs
│   │   │   ├── AdjudicateClaimResponse.cs
│   │   │   ├── ClaimResponse.cs
│   │   │   ├── ClaimLineResponse.cs
│   │   │   └── AdjudicationResultResponse.cs
│   │   ├── Examples/
│   │   │   ├── SubmitClaimRequestExample.cs
│   │   │   ├── SubmitClaimResponseExample.cs
│   │   │   ├── AdjudicateClaimResponseExample.cs
│   │   │   └── ClaimResponseExample.cs
│   │   ├── Middleware/
│   │   │   └── ExceptionMiddleware.cs
│   │   ├── appsettings.json
│   │   └── Program.cs
│   │
│   ├── Adjudicate.Domain/
│   │   ├── Adjudication/
│   │   │   ├── Rules/
│   │   │   │   ├── MemberEligibilityRule.cs
│   │   │   │   ├── PlanCoverageRule.cs
│   │   │   │   └── DuplicateClaimRule.cs
│   │   │   ├── AdjudicationEngine.cs
│   │   │   ├── AdjudicationContext.cs
│   │   │   ├── AdjudicationOutcome.cs
│   │   │   ├── ExistingClaimCheck.cs
│   │   │   ├── IAdjudicationEngine.cs
│   │   │   ├── IAdjudicationRule.cs
│   │   │   └── RuleResult.cs
│   │   ├── Entities/
│   │   │   ├── Claim.cs
│   │   │   ├── ClaimLine.cs
│   │   │   ├── Member.cs
│   │   │   ├── Plan.cs
│   │   │   ├── Coverage.cs
│   │   │   └── AdjudicationResult.cs
│   │   └── Enums/
│   │       ├── AdjudicationDecision.cs
│   │       ├── ClaimStatus.cs
│   │       ├── DenialReasonCode.cs
│   │       └── ServiceType.cs
│   │
│   └── Adjudicate.Infrastructure/
│       ├── Migrations/
│       ├── Models/
│       │   ├── ClaimSubmissionInput.cs
│       │   ├── ClaimLineInput.cs
│       │   ├── ClaimSubmissionResult.cs
│       │   ├── ClaimDetailsResult.cs
│       │   ├── ClaimLineDetails.cs
│       │   └── AdjudicationDetailsResult.cs
│       ├── Persistence/
│       │   ├── Configurations/
│       │   ├── AdjudicateDbContext.cs
│       │   └── AdjudicateDbContextFactory.cs
│       └── Services/
│           ├── IClaimSubmissionService.cs
│           ├── ClaimSubmissionService.cs
│           ├── IClaimAdjudicationService.cs
│           ├── ClaimAdjudicationService.cs
│           ├── IClaimQueryService.cs
│           └── ClaimQueryService.cs
│
└── tests/
    └── Adjudicate.Tests/
        ├── Adjudication/
        │   ├── MemberEligibilityRuleTests.cs
        │   ├── PlanCoverageRuleTests.cs
        │   ├── DuplicateClaimRuleTests.cs
        │   └── AdjudicationEngineTests.cs
        ├── Api/
        │   ├── AdjudicateApiFactory.cs
        │   └── ClaimsControllerTests.cs
        └── Infrastructure/
            ├── MsSqlContainerFixture.cs
            └── ClaimAdjudicationServiceTests.cs
```

---

## 10. Future Enhancements

- **Pricing engine** — apply fee schedules and contracted rates to calculate allowed amounts independently of billed amounts
- **Prior authorization** — enforce pre-approval requirements for certain service types before adjudication
- **Batch adjudication** — process multiple claims in a single request with per-claim outcome tracking
- **Authentication and authorization** — payer/provider roles with JWT-based access control
- **Event-driven processing** — publish adjudication events to a message broker for downstream consumers (remittance, reporting, audit)

---

## 11. Key Engineering Decisions

### Clean Architecture
The solution is split into four projects — `Domain`, `Infrastructure`, `Api`, and `Tests` — with dependency flow enforced at the project reference level. The `Domain` layer has no NuGet dependencies beyond the base runtime. Infrastructure services return application models, not EF-tracked domain entities, so the API layer is decoupled from the persistence layer.

### Rule Engine Pattern
Adjudication rules implement a common `IAdjudicationRule` interface with a single `Evaluate(AdjudicationContext)` method returning a `RuleResult` record. The `AdjudicationEngine` iterates rules in order and short-circuits on the first failure. Adding a new rule requires implementing one interface and registering it in DI — no changes to existing rules or engine logic.

### Testcontainers for Integration Tests
Integration and API tests run against a real SQL Server 2022 instance spun up in Docker by Testcontainers. This eliminates the risk of in-memory database behavior masking real SQL Server incompatibilities (type mapping, migration correctness, index behavior). The container lifecycle is managed by xUnit's `IAsyncLifetime` and shared across test classes via a collection fixture, keeping overhead low.

### Domain-Driven Design Concepts
Aggregates (`Claim`, `Plan`) own their child entities and enforce invariants through private constructors, static factory methods, and encapsulated collections. `Claim.SetResult()` is the only entry point for recording an adjudication outcome — it validates that no prior result exists and that the claim has lines. State mutation is impossible through any other path.

### Duplicate Detection via Projection
The duplicate claim check projects existing claims to a lightweight `ExistingClaimCheck` record (member ID, service date, service codes) before loading them, rather than materializing full `Claim` aggregates. This keeps the query narrow and prevents the engine from receiving EF-tracked objects it has no business touching.

---

## 12. Resume Value

This project demonstrates end-to-end backend engineering in a domain with real business complexity:

- **Domain modeling** with DDD aggregate roots, private constructors, factory methods, and encapsulated invariants
- **Rule engine design** using a composable, testable chain-of-responsibility pattern
- **EF Core advanced configuration** including backing field access, enum-to-string conversion, decimal precision, 1:1 owned results, and unique index management via Fluent API
- **Service layer design** with clear separation between submission, adjudication, and query concerns, and application models that prevent domain entity leakage across boundaries
- **Integration testing discipline** with real SQL Server via Testcontainers, isolated data per test, shared container lifecycle, and full assertion against persisted state
- **API test coverage** using `WebApplicationFactory` to exercise the full HTTP stack including middleware behavior
- **Clean architecture enforcement** through project structure and explicit dependency direction
