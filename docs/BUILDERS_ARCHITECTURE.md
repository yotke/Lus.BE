# Builders Architecture

> Status: **specified 2026-08-18, not yet implemented in Lus.** Ported concepts from ArmyLuz (`docs/BUILDERS_ARCHITECTURE.md` there). Spec: [`docs/superpowers/specs/2026-08-18-ai-builders-port-design.md`](./superpowers/specs/2026-08-18-ai-builders-port-design.md).

## Why

ArmyLuz extracted an entity-agnostic builder kernel so a second builder (Rules) did not copy-paste the Organization builder. Lus takes that kernel and writes a **Document** builder against it. The scheduling org builder itself is **not** ported.

## The five-part anatomy

Every builder — Documents is the first in Lus — has the same five parts:

1. **Draft slice** — a versioned draft mutated *only* via patch batches. `DraftPatcher` applies / undoes / redoes under an optimistic `Version` guard. No direct writes.
2. **Agents wave** — catalog is the single source of truth. Parallel within a wave, sequential across enrichment. Agents are pure functions: stdin JSON `{"Draft":…,"Input":…}` → one PascalCase envelope on stdout. They never touch the DB.
3. **Reconciler** — a final sequential pass resolving cross-agent conflicts.
4. **Suggestions** — chip suggestions via provider fan-out.
5. **Commit mapper** — draft → the entity's **canonical MediatR commands**. Builders never bypass the command layer.

Session state: Redis, 7-day TTL, plus a DB rescue row (`DocumentBuildSessionRow`), schema-versioned so a namespace change self-invalidates stale sessions. `SessionSchemaVersion` may only ever increase.

## Kernel (`Lus.Application/Common/Builders/`)

Ported ~verbatim from `ArmyLuz.Application/Common/Builders/` (611 LOC). Namespace rename `ArmyLuz.* → Lus.*` only. Do not "clean up".

| Type | Role |
|---|---|
| `IBuilderAgentCatalog` + descriptor hierarchy | Six kinds: Content, Validator, Planner, Advisor, Refiner, Importer. Double-dispatch via `IBuilderAgentVisitor<T>` — adding a kind is a compiler-enforced new `Visit` method. |
| `AgentResult<T>` | Success / Failed. Agent failures degrade the slot, not the turn. |
| `BuilderAgentClientCore` | Timeout, envelope parse, failure mapping over `IPythonScriptsAdapter.RunAgentAsync`. |
| `SequentialAgentWaveRunner` | Sequential "run → apply → save → notify". One agent's failure never stops the wave. |
| `BuilderSessionStoreBase<TSession>` | Redis-fast + DB-truth, schema-version discard-not-migrate. |
| `IBuilderEventSender<TPatchOp,TQuestion,TWarning>` | SignalR contract: DraftPatched, AgentStatus, QuestionAsked, BuilderMessage, CommitCompleted, Error. Fire-and-forget. |
| `BuilderTurnContext` | UserId / JobId / Language captured on the HTTP request. |

**ARCH-1:** kernel types are extracted bottom-up once a second builder needs them, never pre-generalized. `IAgentTransport` is *not* pre-abstracted.

## Envelope law (frozen)

See [`PYTHON_AGENTS_BRIDGE.md`](./PYTHON_AGENTS_BRIDGE.md). One line of PascalCase JSON on stdout. Exit 0 for handled failures. Schema validation before emit. Tracebacks to stderr only.

## Adding a builder to entity X (checklist)

1. `Application/X/Builder/` with Orchestration / Agents / Commit / Scaffolding / Models / Services / Entities / Repositories.
2. Draft-slice DTOs in `Contracts/X/Builder/`.
3. Python agents under `PythonScripts/agents/x/` registered as `x.<name>`, plus `agents/schemas/x.<name>.result.schema.json`.
4. An `XBuilderAgentCatalog` declaring waves; orchestration composes the kernel wave executor.
5. Commit mapper → X's canonical MediatR commands only.
6. DI in `Lus.Api/Infrastructure/Extensions/` (manual `AddScoped`, grouped extension).
7. SignalR events reuse `IBuilderEventSender<,,>`. Entity-specific events stay on the entity interface.
8. Guard tests: kernel stays entity-agnostic; X/Builder depends only on the kernel (not on another entity's Builder).

## Guard tests (executable laws)

| Test | Law |
|---|---|
| `CommonBuildersKernel_ReferencesNoEntitySpecificTypes` | Reflection over every kernel type's bases, interfaces, members, params. No `Documents` (or other entity) type may leak into `Common/Builders`. |
| `DocumentsBuilder_DependsOnlyOnTheKernel` | `Documents/Builder` may use the kernel, never the reverse. |
| `SessionSchemaVersion_OnlyEverIncreases` | Lowering would resurrect stale Redis payloads. |
| `DocumentBuilderControllerRouteTests` | The route set is an explicit decision. |
| `test_runner_aliases.py` | Every alias targets a registered agent; `doc.*` ⇒ module in `agents/doc/`; unknown agent still yields a safe envelope. |
| `test_model_router.py` | Every LLM-bearing agent in the registry has a tier row. |

## What is deliberately not here

- ArmyLuz `Organizations/Builder/` (~9500 LOC of scheduling domain).
- The 16 org-specific `PythonScriptsAdapter` spawn sites.
- FastAPI `ArmyLuz.Ai.PythonEngine` (the unwired second lane).
