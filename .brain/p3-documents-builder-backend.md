# P3 — Documents/Builder backend

> Parent plan + [`docs/DOCUMENT_BUILDER.md`](../docs/DOCUMENT_BUILDER.md). Spec §5–6.

**Goal:** Entities, draft model, `DraftPatcher`, session store, orchestrator, catalog, turn endpoint.

**Exit criterion:** A turn applies patches and bumps the version, with undo/redo.

**Depends on:** P1 kernel + adapter. LLM agents are **not** required — P3 can apply patches from a stub agent / classifier that treats any text as "add a note field" until P4.

**Standing assumption:** `DocumentSeries` has **no Year column**. The chain may cross years.

---

### Task 1: Entities + EF + migration

**Files — Create:**

```
src/Lus.Application/Documents/Entities/
  DocumentSeries.cs
  DocumentTemplate.cs
  DocumentInstance.cs
  DocumentRow.cs
  DocumentBuildSessionRow.cs
  RateCard.cs
src/Lus.Application/Documents/Repositories/I*.cs
src/Lus.Infrastructure/Configuration/Document*EntityTypeConfiguration.cs
src/Lus.Infrastructure/Repositories/Document*Repository.cs
```

Field lists are in the spec §5.1. `OrganizationId` on `DocumentSeries`. `DayOfWeek` on `DocumentRow` is stored as derived (Hebrew letter or enum — store `DayOfWeek` as `int` 0–6 computed from `Date`, never from input).

Statuses: `DocumentInstanceStatus { Draft = 0, Committed = 1, Rendered = 2 }`.

- [x] Add DbSets to `ApplicationContext`.
- [ ] `dotnet ef migrations add AddDocumentBuilder --project src/Lus.Infrastructure --startup-project src/Lus.Api`
- [x] Do **not** apply to production from a laptop (`appsettings.Development.json` AutoMigrations=false). Commit the migration.
- [ ] Commit: `feat: add DocumentSeries/Instance/Row/Template/RateCard/session entities`

Entity classes + EF configs are in the model. **Do not generate the migration yet** — Organization / ProjectTemplate / ProjectTime are in the snapshot but omitted from `OnModelCreating`; `ef migrations add` would try to drop them. See `.brain/gotchas.md`.

---

### Task 2: Contracts + DraftPatcher

**Files:**
- Create: `src/Lus.Contracts/Documents/Builder/DraftPatchOp.cs`
- Create: `src/Lus.Contracts/Documents/Builder/DocumentDraftDto.cs`
- Create: `src/Lus.Application/Documents/Builder/Services/DraftPatcher.cs`
- Test: `src/Lus.Api.Tests/Documents/DraftPatcherTests.cs` (or Application.Tests if you add that project)

```csharp
public sealed class DraftPatchOp
{
    public required string Op { get; init; } // AddRow|UpdateRow|RemoveRow|SetField|SetTotals
    public required string Path { get; init; }
    public JsonElement? Value { get; init; }
}
```

`DraftPatcher.Apply(DocumentDraftDto draft, int expectedVersion, IReadOnlyList<DraftPatchOp> ops)`:
- throws / returns conflict if `draft.Version != expectedVersion`
- applies atomically (copy, then swap)
- pushes inverse ops onto an undo stack
- increments `Version`

`Undo` / `Redo` pop the inverse stack.

Tests:
- AddRow then UpdateRow hours → version 2, row present
- stale version rejected
- Undo restores the previous draft JSON (deep-equal)
- Hebrew `Subject` survives apply

- [ ] Commit: `feat: add DraftPatcher with optimistic version and undo/redo`

---

### Task 3: Session store

**Files:**
- Create: `src/Lus.Application/Documents/Builder/Services/DocumentBuildSession.cs` (`IBuilderSession`)
- Create: `src/Lus.Application/Documents/Builder/Services/DocumentBuildSessionStore.cs` : `BuilderSessionStoreBase<DocumentBuildSession>`
- `SessionSchemaVersion = 1` const. Only ever increase.

Cache key prefix: `docbuild:{userId}`. TTL 7 days.

Durable row: `DocumentBuildSessionRow` (Id, UserId, InstanceId, SchemaVersion, Version, DraftJson, UpdatedAt).

- [x] Test: schema version below current is discarded (returns null), not migrated.
- [ ] Commit: `feat: add DocumentBuildSession Redis+DB store`

---

### Task 4: Catalog + orchestrator + turn command

**Files:**
- Create: `src/Lus.Application/Documents/Builder/Agents/DocumentBuilderAgentCatalog.cs`
- Create: `src/Lus.Application/Documents/Builder/Agents/DocumentBuilderAgentClient.cs` (thin wrapper over `BuilderAgentClientCore`)
- Create: `src/Lus.Application/Documents/Builder/Orchestration/DocumentBuilderOrchestrator.cs`
- Create: `src/Lus.Application/Documents/Builder/Commands/RunTurn/RunDocumentBuilderTurnCommand.cs` + Handler
- Modify: `DocumentBuilderController` — `POST v1/documents/builder/turn` and `POST .../undo` `POST .../redo`
- Create: `src/Lus.Api/Infrastructure/Extensions/DocumentBuilderExtensions.cs` (DI)
- Create: SignalR sender implementing `IBuilderEventSender<DraftPatchOp, DocumentQuestionDto, DocumentWarningDto>` — fire-and-forget, never throw

P3 catalog can register:
- `doc.echo` (already exists) as a Content wave-1 placeholder **or** a classifier that maps any text to a single `SetField` on `draft.lastUtterance`
- Real `doc.row_extractor` is P4

Turn flow:
1. Load session (or create empty draft)
2. Apply incoming text as patches from the stub/echo (P3) / agents (P4)
3. Save session
4. `SendDraftPatchedAsync`
5. Return `{ version, ops }`

Tests:
- Two turns bump version 0→1→2
- Undo returns version 1 draft
- Failed agent does not roll back earlier patches in the same wave (`SequentialAgentWaveRunner` law)
- SignalR down: turn still returns ops (fake sender that throws is swallowed)

- [ ] Commit: `feat: add document builder turn with patch versioning`

**P3 done when:** authenticated `POST v1/documents/builder/turn` with `{version:0,text:"hi"}` returns `version:1` and a non-empty `ops` array; `POST undo` restores version 0.
