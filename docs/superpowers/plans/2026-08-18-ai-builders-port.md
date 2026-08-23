# AI Builders Port Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Execute **one phase at a time**. Each phase has its own plan file in `.brain/` — this file is the index.

**Goal:** Port the AI builder kernel from ArmyLuz into Lus and use it to build a Document Builder that fills an exemplar workbook from conversation and emits `.xlsx` / `.pdf`.

**Architecture:** Subprocess Python agents behind `PythonScriptsAdapter.RunAgentAsync`. Entity-agnostic kernel in `Lus.Application/Common/Builders/`. Document builder written fresh against the kernel. One document model, two renderers (openpyxl round-trip for xlsx; page-set PDF). Redis + SignalR for session + live canvas. Auth hardening only — no tenancy work.

**Tech Stack:** .NET 9, Angular 18, Python 3, openpyxl, EasyCaching Redis, SignalR, MediatR, MySQL 8, Docker / Railway.

**Spec:** [`docs/superpowers/specs/2026-08-18-ai-builders-port-design.md`](../specs/2026-08-18-ai-builders-port-design.md)

## Global Constraints

- Namespaces rename `ArmyLuz.* → Lus.*` only on ported files. Do not "clean up" the UTF-8/BOM/cancellation scars.
- Kernel (`Common/Builders`) must never reference Documents (or any entity) types. Enforced by guard tests from P1.
- Agents are pure functions. Exit 0 for handled failures. Stdout = one PascalCase envelope. Stderr = COST/PROGRESS/tracebacks.
- `doc.formatter` and `doc.carry_forward` are LLM-free. Money is computed, never generated.
- `excel-export.component.ts` is frozen. Do not extend it.
- Not porting ArmyLuz scheduling domain, org builder (9507 LOC), FastAPI PythonEngine, tenant middleware, or subdomain routing.
- Workbook series is per client-project and **open-ended** (no year column). Year in the filename is a label.
- Frontend lives in the nested `src/Lus.UI` repo (gitignored from Lus.BE). P2 and P5 land there.
- Hebrew/RTL is the happy path.
- Organization-scoped everything via `IUserAccessor` at the handler layer.
- Python OpenAI key is optional until P4. P0/P1 `doc.echo` is keyless.

## Phase index

| P | Plan file | Exit criterion |
|---|---|---|
| **P0** | [`.brain/p0-redis-signalr-python.md`](../../../.brain/p0-redis-signalr-python.md) | `docker compose up` serves the API with a reachable hub and `python3 -c "import openpyxl"` inside the image |
| **P1** | [`.brain/p1-kernel-adapter-runner.md`](../../../.brain/p1-kernel-adapter-runner.md) | A trivial `doc.echo` agent round-trips Hebrew text end-to-end from a controller |
| **P2** | [`.brain/p2-non-blocking-loading.md`](../../../.brain/p2-non-blocking-loading.md) | No full-screen block anywhere; <300ms requests show nothing |
| **P3** | [`.brain/p3-documents-builder-backend.md`](../../../.brain/p3-documents-builder-backend.md) | A turn applies patches and bumps the version, with undo/redo |
| **P4** | [`.brain/p4-doc-agents-template-reader.md`](../../../.brain/p4-doc-agents-template-reader.md) | The supplied workbook parses into a `DocumentTemplate` with all five blocks identified |
| **P5** | [`.brain/p5-document-builder-ui.md`](../../../.brain/p5-document-builder-ui.md) | Dictating a work-log line renders a new row live on the canvas |
| **P6** | [`.brain/p6-xlsx-renderer.md`](../../../.brain/p6-xlsx-renderer.md) | Golden-file test green against the supplied workbook |
| **P7** | [`.brain/p7-pdf-renderer.md`](../../../.brain/p7-pdf-renderer.md) | Output matches the supplied monthly PDFs |
| **P8** | [`.brain/p8-auth-hardening.md`](../../../.brain/p8-auth-hardening.md) | `[Permission("documents.build")]` enforced; security stamp invalidation works |

## File map (locked)

### P0 — infra

- Modify: `src/docker-compose.yml`, `Dockerfile`, `src/Dockerfile`, `src/Lus.Api/Infrastructure/Extensions/CachingExtensions.cs`, `src/Lus.Api/Lus.Api.csproj`, `src/Lus.Api/appsettings.json`, `src/Lus.Api/appsettings.Development.json`, `src/Lus.Api/appsettings.Production.json`, `src/Lus.Api/Infrastructure/Extensions/EndpointsExtensions.cs`, `docs/DEPLOYMENT_RAILWAY.md`, `docs/ARCHITECTURE.md`
- Create: `src/Lus.Api/Infrastructure/SignalRHubs/DocumentBuilderHub.cs`, `PythonScripts/requirements.txt`, `PythonScripts/.gitkeep` (minimal until P1)

### P1 — kernel + bridge

- Create: `src/Lus.Application/Common/Builders/*` (7 files, namespace rename), `src/Lus.Application/Common/Ports/IPythonScriptsAdapter.cs`, `src/Lus.Infrastructure/Adapters/PythonScriptsWS/PythonScriptsAdapter.cs` (generic runner ONLY), `src/Lus.Api/Infrastructure/Extensions/PythonAdapterExtensions.cs`, `PythonScripts/agents/runner.py`, `PythonScripts/agents/doc/echo.py`, `PythonScripts/agents/schemas/echo.result.schema.json`, `PythonScripts/pyutil/*`, `PythonScripts/core/*`
- Create: `src/Lus.Api/Controllers/DocumentBuilderController.cs` (echo endpoint only)
- Test: `src/Lus.Api.Tests/Builders/BuilderArchitectureGuardTests.cs`, `PythonScripts/tests/test_runner_aliases.py`

### P3 — document domain

- Create: `src/Lus.Application/Documents/**`, `src/Lus.Contracts/Documents/**`, EF configs + migration
- Create: `DraftPatcher`, `DocumentBuildSessionStore`, `DocumentBuilderOrchestrator`, catalog, turn/commit commands

### P4–P7 — agents + render

- Create: `PythonScripts/agents/doc/*.py`, `PythonScripts/render/*.py`, golden fixtures under `PythonScripts/tests/golden/`

### P2 / P5 — frontend (`src/Lus.UI`, nested repo)

- Delete: `Adapters/loader`, `Adapters/Interceptors/blockUI-request`
- Create: `Activities/DocumentBuilder/**`, `Infrastructure/Services/documentBuilderService/**`

## Source of truth for ported files

ArmyLuz at `/Users/onecity/Desktop/projects/ArmyLuz`:

- Kernel: `ArmyLuz.Application/Common/Builders/*.cs`
- Runner method: `ArmyLuz.Infrastructure/Adapters/PythonScriptsWS/PythonScriptsAdapter.RunAgentAsync` (lines ~58–204) — **do not copy the other 16 spawn sites**
- DI: `ArmyLuz.Api/Infrastructure/Extensions/PythonAdapterExtensions.cs`
- Runner: `PythonScripts/agents/runner.py` (registry replaced with `doc.*`)
- pyutil + core: `PythonScripts/pyutil/{model_router,llm_model,credits}.py`, `PythonScripts/core/*`
- Docker python install: ArmyLuz root `Dockerfile` runtime stage
