# 2026-06-07 — Organizations + Projections + Search engine

Goal: bring ArmyLuz's org-scoped flows, projection read-models, and the filter/search engine into Luz. Full design in `/docs/ORG_PROJECTIONS_SEARCH.md`.

## Findings
- **Org flows already exist in Luz** (entities, repos, Get/Create/Modify/ChangeCurrent/AddOrganizationToUser, current-org cache-key `user_organization_id_{userId}`). Mostly need consistent tenant scoping + search.
- **Projections: none yet** in Luz → porting POCO projection + `DataRetriever<TProjection>` pattern.
- **Search engine: none yet** → porting `ArmyLuz.FilterEngine` → `Lus.FilterEngine` (self-contained, ~36 files, expression trees; no external filter libs).

## Decisions
- New project `src/Lus.FilterEngine` (net9, EF 9.0.0 + AutoMapper 12.0.1). **Drop Oracle** (`OracleBooleanSqlFixVisitor` + Oracle.EntityFrameworkCore) — Luz is MySQL/Pomelo.
- Org scoping stays at **handler layer** (auto-inject `OrganizationId` filter in search handlers), matching ArmyLuz — no EF global query filters.
- Establish the pattern on core entities first (Organizations, Users, ProjectsTemplates, Contacts); extend per the recipe in the docs.

## In flight
- FilterEngine port running (background agent). After it lands: add to `Lus.sln`, reference from Application/Infrastructure/Api, then projections → retrievers → search handlers → tests.
