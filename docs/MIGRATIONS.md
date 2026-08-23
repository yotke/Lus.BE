# Database Migrations

MySQL via `Pomelo.EntityFrameworkCore.MySql` 9.x. Migrations live in
`src/Lus.Infrastructure/Migrations/`; the context is
`src/Lus.Infrastructure/Persistence/ApplicationContext.cs`.

## Adding a migration

```bash
cd src
dotnet ef migrations add <Name> \
  --project Lus.Infrastructure/Lus.Infrastructure.csproj \
  --startup-project Lus.Infrastructure/Lus.Infrastructure.csproj \
  --output-dir Migrations
```

**Both `--project` and `--startup-project` are `Lus.Infrastructure`**, not `Lus.Api`. That works
because of the design-time factory below, and it keeps migration generation independent of the API
host's configuration and DI graph.

## Why `ApplicationContextDesignTimeFactory` exists

The runtime registration (`DatabaseExtensions.AddDatabaseContext`) uses
`ServerVersion.AutoDetect(connectionString)` — which **opens a connection**. At design time that
would make every `dotnet ef migrations add` depend on a reachable MySQL: broken in CI, broken on a
fresh clone, and dependent on whichever database happens to be running locally.

`Persistence/ApplicationContextDesignTimeFactory.cs` pins `MySqlServerVersion 8.0` (matching
`docker-compose.yml` and Railway) and uses a placeholder connection string that is never connected
to. It is discovered only by the EF tooling via `IDesignTimeDbContextFactory<T>`, so **runtime
behaviour is unchanged**.

To scaffold against a real database instead, set `LUS_DESIGN_TIME_CONNECTION`.

## The drift guard

`Lus.Api.Tests/Documents/DocumentSchemaMigrationTests.Model_has_no_pending_changes_beyond_the_last_migration`
diffs the migrations snapshot against the design-time model and **fails when an entity or
`EntityTypeConfiguration` change has not been captured in a migration**. Verified to actually
detect drift (adding a throwaway property fails it; removing it passes).

If it fails, the fix is to add the migration — never to weaken the test.

Two implementation notes worth keeping:

- It diffs the **design-time** model, not `context.Model`. The read-optimized runtime model drops
  configuration the differ needs and throws *"The requested configuration is not stored in the
  read-optimized model"* on `Collation`.
- `IDesignTimeModel` is resolved by scanning loaded `Microsoft.EntityFrameworkCore*` assemblies,
  because the interface does not bind by name from the test project's transitive reference and has
  moved between EFCore and EFCore.Relational across versions.

## Applying migrations

```bash
# preview the SQL for a range (idempotent = safe to re-run)
dotnet ef migrations script <From> <To> \
  --project Lus.Infrastructure/Lus.Infrastructure.csproj \
  --startup-project Lus.Infrastructure/Lus.Infrastructure.csproj \
  --idempotent -o migration.sql
```

Review the SQL before applying to a shared or production database. Railway deploys apply migrations
per `docs/DEPLOYMENT_RAILWAY.md`.

## History

| Migration | Contents |
|---|---|
| `20230910151021_Start-App-Luz-Migration` | initial schema |
| `20230918052916_add-field-manager-to-projects` | project manager field |
| `20231128080653_new-field-to-projectTemplate` | project template field |
| `20260614024305_SyncModelToDb` | .NET 9 / cookie-auth sync |
| `20260818064713_AddDocumentBuilderTables` | **document builder** — 7 tables, 7 indexes, purely additive (no changes to existing tables) |

### `AddDocumentBuilderTables` (2026-08-18)

Tables: `DocumentSeries` · `DocumentTemplates` · `DocumentInstances` · `DocumentDays` ·
`DocumentRows` · `DocumentBuildSessions` · `RateCards`.

Two relationships carry design decisions and are pinned by their own tests:

- **`DocumentInstances.CarryInFromInstanceId` is a self-referencing FK.** The hours-balance chain is
  an *entity relationship*, not a cell address — that is what makes the exemplar's hand-typed
  cross-sheet reference (and its whitespace-sensitive sheet names) structurally impossible. See
  spec §3.3 and concept C7.
- **`DocumentRows` → `DocumentDays` → `DocumentInstances`** is the two-level data band. Archetype 2
  groups time segments into days; archetype 1 is the degenerate one-segment-per-day case. See
  spec §3.5 and C1a.
