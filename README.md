# Pinventory

[![codecov](https://codecov.io/github/Ermesx/pinventory/graph/badge.svg?token=AIIZ24G5IU)](https://codecov.io/github/Ermesx/pinventory)
[![.NET](https://github.com/Ermesx/pinventory/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Ermesx/pinventory/actions/workflows/dotnet.yml)

| Description                                                                                                                                                                                                                                                                           | Coverage                                                                                     |
|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------|
| A multi-service application for managing and exploring "pins" and related metadata. Pinventory uses .NET 9 and .NET Aspire to orchestrate a local development topology with a web front end, domain APIs, background workers, and shared infrastructure like PostgreSQL and RabbitMQ. | ![CodeCov](https://codecov.io/github/Ermesx/pinventory/graphs/sunburst.svg?token=AIIZ24G5IU) |

---

## Architecture

Pinventory follows a microservices-style architecture orchestrated via .NET Aspire. The Aspire AppHost starts the full application graph in development, wiring services to shared resources and a reverse proxy.

Main components:
- **AppHost (Aspire)**: Orchestrates services, provisions local resources (PostgreSQL, RabbitMQ), sets up reverse proxy (YARP), exposes the Aspire dashboard, and provides Scalar API documentation.
- **Web frontend** (src/Pinventory.Web): Blazor Server app using ASP.NET Core Identity and Google authentication; calls internal APIs via typed HTTP clients (Refit).
- **Identity**
  - Domain (src/Pinventory.Identity): Identity models and EF Core data access
  - gRPC Service (src/Pinventory.Identity.Tokens.Grpc): gRPC service for token operations
- **Pins domain** (DDD-style layered architecture)
  - API (src/Pinventory.Pins.Api): Minimal API hosting pin-related endpoints; uses Wolverine for messaging
  - Domain (src/Pinventory.Pins.Domain): Domain models, aggregates (Pin, TagCatalog), value objects, and business logic
  - Application (src/Pinventory.Pins.Application): Command handlers and application services using Wolverine
  - Infrastructure (src/Pinventory.Pins.Infrastructure): EF Core DbContext, entity configurations, and data access
  - Workers
    - Import (src/Pinventory.Pins.Import.Worker): Initiates and checks Google Data Portability archives for starred places
    - Tagging (src/Pinventory.Pins.Tagging.Worker): Applies tagging/classification to pins in the background
- **Notifications**
  - API (src/Pinventory.Notifications.Api): Minimal API for notifications
  - Domain (src/Pinventory.Notifications): NotificationInbox and Notification entities with EF Core DbContext
- **Shared libraries**
  - ApiDefaults (src/Pinventory.ApiDefaults): Common API pipeline, JWT authentication, and endpoint conventions
  - ServiceDefaults (src/Pinventory.ServiceDefaults): Cross-cutting service configuration (observability, resilience, service discovery)
  - Google (src/Pinventory.Google): Google integration helpers (OAuth, Data Portability client)
  - Testing (src/Pinventory.Testing): Shared test infrastructure (Testcontainers, test authentication)
- **Migration Service** (src/Pinventory.MigrationService): Worker responsible for database schema migrations at startup

**Development Tools:**
- **Scalar**: Interactive API documentation accessible at `/scalar/` via the reverse proxy
- **PgWeb**: PostgreSQL web interface available on port 5050 for database inspection

High level: The Web app authenticates users (Google, Identity), then communicates with domain APIs (Pins, Notifications) through a YARP reverse proxy. Workers process background tasks. Wolverine handles messaging and command processing with durable outbox/inbox patterns. The AppHost coordinates everything for local development.

---

## Technology Stack

**Core Framework:**
- .NET 9 (C#)
- ASP.NET Core Minimal APIs and Blazor Server

**Data & Persistence:**
- Entity Framework Core with Npgsql (PostgreSQL)
- Wolverine for messaging, command handling, and durable outbox/inbox patterns
- RabbitMQ for message broker (via Aspire resource)

**Authentication & Authorization:**
- ASP.NET Core Identity with Google OAuth
- JWT Bearer authentication for API-to-API communication

**Infrastructure & Orchestration:**
- .NET Aspire (AppHost, resources, dashboard, YARP reverse proxy)
- PostgreSQL with persistent volumes
- RabbitMQ with management plugin and persistent volumes
- PgWeb for database inspection

**API & Communication:**
- Refit for typed HTTP clients
- gRPC for internal token service
- Scalar for interactive API documentation
- OpenAPI/Swagger document generation

**Messaging & Serialization:**
- Wolverine with RabbitMQ transport
- MemoryPack for message serialization
- FluentResults for error handling

**Observability:**
- OpenTelemetry for distributed tracing and metrics

**Testing:**
- TUnit for unit and integration testing
- Testcontainers (PostgreSQL, RabbitMQ) for integration tests
- Shouldly for assertions
- Moq for mocking

---

## Features

**Pin Management:**
- Import starred Google Maps places via Google Data Portability
- Track pin status (Unknown, Open, Closed, TemporaryClosed)
- Manage pin metadata (address, location, tags)

**Tag Catalog:**
- Define custom tag catalogs per user
- Add and remove tags from catalogs
- Normalized tag handling (trimming, deduplication, case-insensitive)

**Authentication:**
- Google OAuth integration with incremental consent
- Secure token storage and management
- JWT-based API authentication

**Background Processing:**
- Asynchronous import of Google Data Portability archives
- Background tagging/classification workers
- Durable message processing with outbox/inbox patterns

---

## Getting Started

1) Clone the repository

```bash
git clone https://github.com/ermesx/pinventory.git
cd pinventory
```

2) Restore dependencies

```bash
dotnet restore
```

3) Run the full application using the Aspire AppHost

```bash
dotnet run --project src/Pinventory.AppHost
```

- The console will print service URLs. The reverse proxy and the Aspire dashboard links will be shown at startup.
- The Web application will be available at the URL shown in the console (ports may vary per run).
- The Aspire dashboard provides a topology view, logs, and health—open the link printed by AppHost.

---

## Project Structure

```
pinventory/
├── src/                                        # Source projects
│   ├── Pinventory.AppHost/                     # Aspire orchestration
│   ├── Pinventory.Web/                         # Blazor Server frontend
│   ├── Pinventory.Identity/                    # Identity domain
│   ├── Pinventory.Identity.Tokens.Grpc/        # Token gRPC service
│   ├── Pinventory.Pins.Api/                    # Pins API
│   ├── Pinventory.Pins.Domain/                 # Pins domain models
│   ├── Pinventory.Pins.Application/            # Pins application layer
│   ├── Pinventory.Pins.Infrastructure/         # Pins data access
│   ├── Pinventory.Pins.Import.Worker/          # Import background worker
│   ├── Pinventory.Pins.Tagging.Worker/         # Tagging background worker
│   ├── Pinventory.Notifications/               # Notifications domain
│   ├── Pinventory.Notifications.Api/           # Notifications API
│   ├── Pinventory.ApiDefaults/                 # Shared API configuration
│   ├── Pinventory.ServiceDefaults/             # Shared service configuration
│   ├── Pinventory.Google/                      # Google integration
│   ├── Pinventory.Testing/                     # Shared test infrastructure
│   └── Pinventory.MigrationService/            # Database migrations
├── tests/                                      # Test projects
│   ├── Directory.Build.props                   # Centralized test configuration
│   ├── Pinventory.Pins.Api.IntegrationTests/   # Integration tests for Pins API
│   ├── Pinventory.Pins.UnitTests/              # Unit tests for Pins domain and application logic
│   └── Pinventory.Identity.UnitTests/          # Unit tests for Identity and token services
├── doc/                                        # Domain documentation
│   ├── pins-ubiquitous-language.md
│   └── pins-invariants.md
├── Directory.Build.props                       # Central build configuration
├── Directory.Packages.props                    # Centralized package versions
└── .editorconfig                               # Code style settings
```

---

## Configuration

Pinventory uses standard ASP.NET Core configuration (appsettings.json, environment variables, user secrets). When running via AppHost, many service-to-resource settings (like DB connection strings) are provided automatically.

User secrets (recommended during local development):
- Google authentication for Web

```bash
# Initialize and set secrets for the Web project
# From repo root:
dotnet user-secrets --project src/Pinventory.Web set "Authentication:Google:ClientId" "<your-client-id>"
dotnet user-secrets --project src/Pinventory.Web set "Authentication:Google:ClientSecret" "<your-client-secret>"
dotnet user-secrets --project src/Pinventory.Web set "Pinventory:ProjectId" "<your-project-id>"
```

---

## Development Workflow

- Run everything (recommended):

```bash
dotnet run --project src/Pinventory.AppHost
```

- Run an individual service (examples):

```bash
dotnet run --project src/Pinventory.Pins.Api
# or
dotnet run --project src/Pinventory.Web
```

- Testing

```bash
dotnet test

# Run with coverage (requires dotnet-coverage)
dotnet tool install --global dotnet-coverage
dotnet-coverage collect "dotnet test" -f cobertura -o coverage.xml
```

- Code formatting and style
  - The repository enforces conventions via .editorconfig.
  - Run formatters/IDE analyzers or use:

```bash
dotnet format
```

- Branching
  - No strict convention enforced; suggested: feature/<short-name>, fix/<short-name>, chore/<short-name>.

---

## Testing

The solution includes comprehensive unit and integration tests:

**Test Projects:**
- `Pinventory.Pins.Api.IntegrationTests`: Integration tests for Pins API using Testcontainers
- `Pinventory.Pins.UnitTests`: Unit tests for Pins domain and application logic
- `Pinventory.Identity.UnitTests`: Unit tests for Identity and token services
- `Pinventory.Testing`: Shared test infrastructure (Testcontainers, authentication helpers)

**Running Tests:**

```bash
# Run all tests
dotnet test

# Run with coverage (requires dotnet-coverage)
dotnet tool install --global dotnet-coverage
dotnet-coverage collect "dotnet test" -f cobertura -o coverage.xml
```

**Testing Approach:**
- TUnit framework for test execution
- Testcontainers for PostgreSQL and RabbitMQ in integration tests
- Wolverine solo mode for faster integration test startup
- Shouldly for fluent assertions

---

## Development Tools

**Aspire Dashboard:**
- Accessible when running the AppHost
- Provides topology view, logs, traces, and health checks

**Scalar API Documentation:**
- Interactive API documentation at `http://localhost:9000/scalar/`
- Aggregates OpenAPI specs from Pins and Notifications APIs

**PgWeb:**
- PostgreSQL web interface at `http://localhost:5050`
- Inspect database schemas, run queries, view data

**Container Management:**
- PostgreSQL and RabbitMQ use persistent volumes
- Data persists across container restarts

---

## CI/CD

GitHub Actions workflow is defined in `.github/workflows/dotnet.yml` to build and test on pushes and pull requests. Code coverage is reported to CodeCov. Ensure your changes build locally and pass tests before opening a PR.

---

