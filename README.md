# Pinventory

A multi-service application for managing and exploring "pins" and related metadata. Pinventory uses .NET 9 and .NET Aspire to orchestrate a local development topology with a web front end, domain APIs, background workers, and shared infrastructure like PostgreSQL and RabbitMQ.

---

## Architecture

Pinventory follows a microservices-style architecture orchestrated via .NET Aspire. The Aspire AppHost starts the full application graph in development, wiring services to shared resources and a reverse proxy.

Main components:
- AppHost (Aspire): Orchestrates services, provisions local resources (PostgreSQL, RabbitMQ), sets up reverse proxy (YARP), and exposes the Aspire dashboard.
- Web frontend (src/Pinventory.Web): Razor Components (server) app using ASP.NET Core Identity and Google authentication; calls internal APIs via typed HTTP clients (Refit).
- Identity (src/Pinventory.Identity, src/Pinventory.Identity.Tokens.Grpc): Identity models and EF Core data access; gRPC service for token operations.
- Pins domain
  - API (src/Pinventory.Pins.Api): Minimal API hosting pin-related endpoints.
  - Workers
    - Data Sync (src/Pinventory.Pins.DataSync.Worker): Synchronizes pin data from external sources.
    - Tagging (src/Pinventory.Pins.Taging.Worker): Applies tagging/classification to pins in the background.
- Notifications (src/Pinventory.Notifications.Api): Minimal API for notifications.
- Shared libraries
  - ApiDefaults (src/Pinventory.ApiDefaults): Common API pipeline and endpoint conventions.
  - ServiceDefaults (src/Pinventory.ServiceDefaults): Cross-cutting service configuration (observability, resilience, discovery, etc.).
  - Google (src/Pinventory.Google): Google integration helpers (e.g., token endpoint client).
- Migration Service (src/Pinventory.MigrationService): Worker responsible for database schema migrations at startup.

High level: The Web app authenticates users (Google, Identity), then communicates with domain APIs (Pins, Notifications) through a reverse proxy. Workers process background tasks. The AppHost coordinates everything for local development.

---

## Technology Stack

- .NET 9 (C#)
- ASP.NET Core Minimal APIs and Razor Components (Server)
- ASP.NET Core Identity with Google authentication
- Entity Framework Core with Npgsql (PostgreSQL)
- .NET Aspire (AppHost, resources, dashboard, YARP reverse proxy)
- Refit typed HTTP clients
- gRPC for internal token service
- RabbitMQ for messaging (via Aspire resource)
- OpenTelemetry for observability

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

Common solution/build configuration:
- Directory.Build.props: Central TargetFramework (net9.0), nullable, implicit usings
- Directory.Packages.props: Centralized package version management (ManagePackageVersionsCentrally)
- .editorconfig: Code style and analyzer settings across the solution

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
  - Test projects can live under a /tests folder. Once present, run:

```bash
dotnet test
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

## CI/CD

GitHub Actions workflow is defined in `.github/workflows/dotnet.yml` to build and test on pushes and pull requests. Ensure your changes build locally and pass tests before opening a PR.

---

