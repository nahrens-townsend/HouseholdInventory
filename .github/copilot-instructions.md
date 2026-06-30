# Copilot Instructions

## Domain Purpose

This application is a Household Inventory & Warranty Tracking system.

It allows users to:

- Track household items (electronics, appliances, furniture, etc.)
- Store purchase details and warranty expiry dates
- Organize items by house and room
- (Future) send reminders for expiring warranties

This is a real-world CRUD + relational data system designed to simulate enterprise backend development patterns.

## Build & Run

```bash
# Build the solution
dotnet build HouseholdInventory.slnx

# Run the API (Swagger UI available at /swagger)
dotnet run --project src/Inventory.Api

# Apply EF migrations (run from repo root)
dotnet ef database update --project src/Inventory.Infrastructure --startup-project src/Inventory.Api
```

## Architecture

Three-project layered architecture:

- **Inventory.Core** — Domain entities only. No dependencies on other projects.
- **Inventory.Infrastructure** — EF Core `InventoryDbContext` and migrations. References Core.
- **Inventory.Api** — ASP.NET Core controller-based API. References both Core and Infrastructure.

The API injects `InventoryDbContext` directly into controllers (no repository layer). The DB context is defined in Infrastructure but the EF Design package is on the Api project so migrations are generated with `--startup-project src/Inventory.Api`.

## Architecture Rules

- Do NOT introduce repository pattern unless explicitly requested.
- Do NOT introduce service layer unless business logic becomes non-trivial.
- Controllers may use DbContext directly.
- Keep architecture intentionally simple (Clean Architecture lite).
- Avoid overengineering (no CQRS, no MediatR unless requested).

## EF Core Rules

- All decimal fields must explicitly define precision:
  Use `.HasPrecision(18,2)` in Fluent API OR `[Column(TypeName = "decimal(18,2)")]`.
- Do not rely on EF conventions for database schema.
- All relationships must be explicitly configured in `OnModelCreating`.
- Always ensure migrations are reviewed before applying.

## API Design Rules

- Use RESTful conventions:
  - GET /items
  - GET /items/{id}
  - POST /items
  - PUT /items/{id}
  - DELETE /items/{id}

- Return IActionResult (not concrete types).
- Use DTOs for all request models.
- Do not expose EF entities directly in request/response contracts.
- Prefer explicit route names over conventions.

## Debugging & Development Rules

- Prefer running `dotnet build` before EF migrations.
- If EF commands fail, verify correct `--project` and `--startup-project`.
- Always ensure API project is not running before database operations.

## Key Conventions

- **Controller-based routing** (not minimal APIs). Controllers live in `src/Inventory.Api/Controllers/` and use attribute routing (e.g., `[Route("items")]`).
- **Request models** are separate DTOs in `src/Inventory.Api/Models/` (e.g., `CreateInventoryItemRequest`). Do not use entities directly as request bodies.
- **Entities** live in `src/Inventory.Core/Entities/`. Use `[Column(TypeName = "decimal(18,2)")]` on any `decimal` property.
- **Nullable reference types** are enabled across all projects. All non-optional string properties should be initialized to `string.Empty`; optional strings use `string?`.
- **Database**: SQL Server. Connection string key is `"Default"`. Development settings in `appsettings.Development.json` target `Server=localhost;Database=HouseholdInventoryDb` with Windows auth.
- **EF Migrations** live in `src/Inventory.Infrastructure/Migrations/`. Always add migrations with `--project src/Inventory.Infrastructure --startup-project src/Inventory.Api`.
- Target framework is **net10.0**.

## Naming Conventions

- Entities: PascalCase singular (e.g., InventoryItem, Room)
- DbSet properties: plural (e.g., InventoryItems)
- DTOs: suffixed with Request or Response
- Controllers: plural resource names (ItemsController, RoomsController)

## Code Style Preferences

- Keep methods small and readable.
- Prefer explicit code over clever abstractions.
- Avoid unnecessary async/await if not needed.
- Use meaningful variable names (no abbreviations like "itm", "ctx").

## Future Features (Do not implement yet)

- Rooms and Houses hierarchy
- Warranty expiration notifications (background service)
- GraphQL API using HotChocolate
- Azure deployment (App Service + Azure SQL)
- Microservice split (Reminder service)
