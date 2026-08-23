# Luz — Architecture Overview

Luz is a multi-tenant (organization-scoped) line-of-business platform: an ASP.NET Core 9 backend + an Angular frontend, kept together in **one git repository** (`src/Lus.UI` lives beside the backend projects — nothing is split into separate repos).

## Solution layout (`src/Lus.sln`)

| Project | Type | Responsibility |
|---|---|---|
| `Lus.Api` | ASP.NET Core 9 Web API | Entry point, controllers, `Startup`/DI wiring, IdentityServer4 host, cookie auth, Swagger, Hangfire, SignalR |
| `Lus.Application` | Class lib | Business logic: MediatR commands/queries, entities, services, validators, **projections** |
| `Lus.Infrastructure` | Class lib | EF Core (`ApplicationContext`), repositories, **retrievers**, caching, IdentityServer integration |
| `Lus.Authorization` | Class lib | Auth cross-cutting: cookie auth, CSRF, `IUserAccessor`/`IProjectUser`, claims, policies |
| `Lus.Contracts` | Class lib | DTOs, response models, enums (no external deps) |
| `Lus.NotificationCenter` | Class lib | Email/SMS notifications + templates |
| `Lus.FilterEngine` | Class lib | Generic expression-tree **search/filter engine** (ported from ArmyLuz) |
| `Lus.UI` | Angular 18 | Frontend SPA (cookie-based auth) |
| `Lus.Api.Tests` | xUnit | Backend tests (auth, search, handlers) |

## Platform

- **.NET 9** across all backend projects (`global.json` → SDK `9.0.200`, `rollForward: latestFeature`).
- **MySQL** via `Pomelo.EntityFrameworkCore.MySql` (9.x). Hosted on Railway in production.
- **Angular 18** frontend.
- **EasyCaching** for caching. In-memory when `Caching:ProviderName=default`; Redis when `ProviderName=redis` (P0 of the AI builders port). Builder sessions use Redis with a DB rescue row.
- **SignalR** hubs: `/citiesStreetsHub` (legacy) and `/hub/document-builder` (Document Builder).

## Cross-cutting patterns

- **CQRS-lite via MediatR** — every operation is a `Command`/`Query` + handler.
- **Repository pattern** over EF Core in `Lus.Infrastructure`.
- **Organization (tenant) scoping** — applied at the handler layer (see `ORG_PROJECTIONS_SEARCH.md`).
- **Projections + retrievers** — read models for efficient, shaped queries (see `ORG_PROJECTIONS_SEARCH.md`).
- **AutoMapper** for Entity/Projection → DTO mapping.

## Docs index

- [`AUTH_COOKIE_LOGIN.md`](./AUTH_COOKIE_LOGIN.md) — cookie-based login (frontend + backend).
- [`AUTH_HARDENING.md`](./AUTH_HARDENING.md) — P8 permission RBAC, security stamp, headers (no tenancy).
- [`BUILDERS_ARCHITECTURE.md`](./BUILDERS_ARCHITECTURE.md) — AI builder kernel, envelope law, add-a-builder checklist.
- [`DOCUMENT_BUILDER.md`](./DOCUMENT_BUILDER.md) — document series, balance chain, agents, renderers.
- [`PYTHON_AGENTS_BRIDGE.md`](./PYTHON_AGENTS_BRIDGE.md) — subprocess contract, UTF-8 scars, model routing.
- [`NON_BLOCKING_LOADING.md`](./NON_BLOCKING_LOADING.md) — three loading tiers; delete the blocking overlay.
- [`DEPLOYMENT_RAILWAY.md`](./DEPLOYMENT_RAILWAY.md) — Docker + Railway + shiftiz.com domain.
- [`ORG_PROJECTIONS_SEARCH.md`](./ORG_PROJECTIONS_SEARCH.md) — organization scoping, projections, and the filter/search engine.
- Spec: [`superpowers/specs/2026-08-18-ai-builders-port-design.md`](./superpowers/specs/2026-08-18-ai-builders-port-design.md)
- Plan: [`superpowers/plans/2026-08-18-ai-builders-port.md`](./superpowers/plans/2026-08-18-ai-builders-port.md)
- [`MIGRATIONS.md`](./MIGRATIONS.md) — EF migrations, the design-time factory, and the model-drift guard.
