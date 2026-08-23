# Design — AI Builders Port (ArmyLuz → Lus) + the Document Builder

> Date: 2026-08-18 · Status: **APPROVED · plans written · P0 in progress**
> Research: [`.brain/2026-08-18-armyluz-to-lus-port-research.md`](../../../.brain/2026-08-18-armyluz-to-lus-port-research.md)
> Source project: `/Users/onecity/Desktop/projects/ArmyLuz`

---

## 1. Purpose

Bring the AI builder subsystem from ArmyLuz into Lus — **the concepts, not just the plumbing** —
and use it to build the thing Lus actually needs: a **Document Builder** where the user uploads an
exemplar workbook to show how the output should look, then *talks* to the system, and the system
fills that document and emits `.xlsx` or `.pdf`.

This replaces manual data entry (`create-project` / `data-loader` / `project-validator` forms) and
the hand-rolled 1162-line browser Excel writer as the path for new documents.

### 1.1 Non-goals

- Not porting ArmyLuz's scheduling domain (orgs, shifts, persons, rules, assignments, engines).
- Not making Lus multi-tenant with org subdomains (decided: stay as-is).
- Not touching or extending `excel-export.component.ts` — it stays as the legacy `ProjectTemplate`
  export flow, frozen.
- Not building an AI that invents billing figures. Money values are computed deterministically.

---

## 2. Decisions (locked)

| # | Decision | Rationale |
|---|---|---|
| D1 | **Subprocess transport** — port `PythonScriptsAdapter` + `runner.py` | The battle-tested lane, the one actually wired in ArmyLuz; every UTF-8/cancellation scar already fixed. `ArmyLuz.Ai.PythonEngine` (FastAPI) is the less-proven half. |
| D2 | **One document model, two renderers** | The exemplar PDFs are literally the print of the sheet. Same draft → `.xlsx` renderer and `.pdf` renderer. |
| D3 | **Add Redis; extend the existing SignalR** | The live canvas, narrated progress, and the builder session store all assume them. **Correction to research §0: SignalR is already wired in Lus** — `AddSignalR` at `src/Lus.Api/Startup.cs:98`, `MapHub<CitiesStreetsHub>` at `Infrastructure/Extensions/EndpointsExtensions.cs:52`. Only Redis and the Python runtime are genuinely absent. |
| D4 | **Auth hardening only, no org/tenancy work** | Keep per-handler org scoping. RBAC + security stamp + AuthState + reauth + headers + Apple sign-in. |
| D5 | **Server-side rendering via openpyxl round-trip** | "Look exactly like my example" is a round-trip problem. Reopen the exemplar and write into it; never rebuild 31–45 merged ranges by hand. |
| D6 | **The builder unit is the workbook (a series), not one sheet** | The exemplar chains a hours-balance across months by cross-sheet reference. |

### 2.1 Standing assumption (unanswered question)

**A workbook series is per client-project and open-ended; the year in the filename is a label, not a
boundary.** Evidence: the supplied file is named `…ר.ש.ת 2026-אא.xlsx` yet contains sheets from
December 2024 onward, and the balance chain crosses the 2025→2026 year boundary unbroken.

Consequence: `DocumentSeries` has no year column and the carry-forward chain may cross years. If
this is wrong, the correction is a `Year` column on `DocumentSeries` plus a chain-break rule at the
January sheet — contained to P3, not a redesign.

---

## 3. Domain, from the exemplars

Source: `/Users/onecity/Downloads/attachments` — `חשבון שעות לפרויקטים ר.ש.ת 2026-אא.xlsx`
(19 sheets) + three monthly PDFs.

### 3.1 The document

A monthly work-hours account (`דוח ביצוע שעות עבודה`) from a practice to a client. One sheet per
month; each printed to PDF becomes the signed invoice (`ח-ן`). Filename pattern
`DDMMYY01 <client> <month> <year> ח-ן.pdf`.

### 3.2 The five blocks of a month sheet

| Block | Rows (Mar 2026) | Content |
|---|---|---|
| Title | 2, 4 | client name; `דוח ביצוע שעות עבודה <month year>` |
| Letterhead | 5–7 | practice name, address, phone, VAT id; `לקוח`; date; `המתכנן`; `מספר ח-ן` |
| Table header | 10 | `תאריך │ יום השבוע │ סהכ שעות עבודה │ מיקום │ נושא העבודה` |
| **Data band** | 11 → N | one row per work item; **A and B merged vertically across rows sharing a date** |
| Totals | N+1 | `סה"כ` · `=SUM(C…)` · carry-in · remaining |
| Billing | +3…+7 | total hours · rate · subtotal · VAT 18% · total |
| Declaration | +9…+16 | declaration text, signatures, planner + municipality approval |

All sheets `rightToLeft=True`. Column widths fixed: `A=25.6, B=20.6, D=15.6, E=77.5, F=8.6`.
31–45 merged ranges per sheet.

### 3.3 The balance mechanic

```
C = SUM(data band)                     hours consumed this month
D = ='<previous month sheet>'!E<row>   balance carried in
E = D - C                              balance remaining
```
Mar 2026: `C25=SUM(C11:C24)=32`, `D25=+'פברואר  2026 '!E31=760`, `E25=728`.

### 3.4 Defects in the current manual process (the value case)

1. Cross-sheet refs hand-typed to a row that moves monthly (E14, E22, E31, E45…).
2. Sheet names carry inconsistent leading/trailing/double whitespace that those formulas must match
   byte-exact (`'מרץ 2025 '`, `'אפריל 2025  '`, `' אפריל 2026 '`, `'פברואר  2026 '`).
3. Layout drift: the billing block sat in column C through 2025 and moved to column D in 2026.
4. Hand-typed duplicates of computed values (`D28=32` beside `C25=SUM(C11:C24)=32`).
5. **Live money bug** — Feb and Mar 2026 have an empty rate / subtotal / VAT, so `D32=D31+D30`
   prints **`0.00`** on the issued PDFs. The rate (225) lives only on the stale `תעריפים` sheet.
6. `תעריפים` is a stale Jan-2025 copy doing double duty as master template and rate store.

Defects 1–3 are killed by D6 (the series owns the chain). Defects 4–5 are killed by "derived values
are computed, never typed" plus a validator lint. Defect 6 is killed by moving rates into entities.

### 3.5 Archetype 2 — the municipal project estimate register

Second exemplar batch (`/Users/onecity/Downloads/attachments (1)`):
`אומדן פרוייקטים חדש שוטף-2026.xls` — legacy BIFF `.xls`, **35 sheets** (Jul 2022 → Jun 2026),
~450 rows each. Client `עירית תל אביב`, `אגף דרכים ומאור`.

Same family as archetype 1 — letterhead, table, totals, billing, declaration — but four structural
differences that the design must absorb rather than special-case:

| Difference | Archetype 1 | Archetype 2 |
|---|---|---|
| Cardinality | one report per sheet | **~11 report blocks stacked per sheet**, ~39 rows each, one per project |
| Data band | one row per work item | **two-level**: time segments (`start`/`end` as Excel time fractions) grouped into days; the day total is written once and merged down |
| Billing chain | hours × rate → VAT 18% → total | hours × rate → **less 1% plots (`פלוטים 1%-`)** → VAT 18% → total |
| Contract | none | `מספר חוזה 202-22-746` + validity `2022-07-01 → 2026-07-01` |

Mar 2026, first block: segments 7h+3h, 8h, 6h+2h, 6h → day totals summing to 32; carried in 2815,
remaining 2783. Rate 223.97 → 7167.04 → less plots 7095.37 → VAT 1277.17 → **8372.54**.

Month gaps are real in this series (Aug 2023 → Sep 2024, Jun 2025, Apr 2026 all missing).

**Consequences:**

1. **`DocumentInstance` maps to a block within a sheet, not to a sheet.** `DocumentTemplate` gains
   `BlockHeight` and `RepeatPolicy`.
2. **The billing formula set is template data, never code.** Two archetypes already disagree on it.
3. **The data band is two-level.** `doc.row_extractor` emits segments; `doc.formatter` derives the
   day total. Neither shape is hard-coded.
4. **New validator rule:** no row dated outside contract validity.
5. **`.xls` (BIFF) needs a legacy read path.** `openpyxl` cannot open it — the importer uses `xlrd`
   to read, and round-trip rendering of a `.xls` source requires a convert-to-`.xlsx` step first
   (`xlrd` cannot write).
6. **Gap detection (C7) is a requirement**, evidenced not speculated.

### 3.6 `מקור` is a manual backup, not a template

Both exemplar workbooks ship with a `מקור` ("source") twin. A full cell-by-cell diff of the `.xlsx`
pair returns **0 differing cells** across all 19 sheets; the `.xls` pair differs only by container
padding.

So `מקור` is **version control by filename copy** — no history, no diff, no authoritative copy
beyond the filename. Add it to the defect list in §3.4. It is replaced by real versioning:
`DocumentInstance.Status` plus the draft's monotonic `Version` and inverse patch batches.

---

## 3.7 Platform versions (checked 2026-08-18, not assumed)

| | Lus | ArmyLuz (port source) | Latest |
|---|---|---|---|
| .NET SDK | **9.0.301** | 9 | 9 |
| Angular | **18.1.0** (CLI 18.1.2, TS 5.5.2, zone.js 0.14.3) | **20.0.0** (TS 5.8.2, zone.js 0.15.1) | 22.1.2 |
| `@microsoft/signalr` | absent | ^10.0.0 | — |

| # | Decision | Rationale |
|---|---|---|
| D7 | **.NET stays on 9** | Lus is already on 9.0.301 with `rollForward: latestFeature`. Nothing to do. |
| D8 | **Angular 18 → 20, not → 22** | The FE being ported is written against Angular 20 and signalr v10. Landing on 20 means ported components compile as written; landing on 22 means migrating the same code twice. Two majors instead of four. 20→22 becomes a separate low-risk follow-up once the port is green. |

---

## 4. Architecture

```
Angular
  Activities/DocumentBuilder/
    document-builder.component        shell
    doc-canvas/                       live sheet preview (the "canvas")
    chat rail + agent ticker + question chips
  Infrastructure/Services/documentBuilderService/
    document-builder-api.service · document-builder-state.service
    builder-payload.normalizer · builder.types
        │ POST v1/documents/builder/turn
        │ SignalR hub/document-builder
        ▼
Lus.Api
  Controllers/DocumentBuilderController
  Infrastructure/Extensions/{PythonAdapterExtensions, DocumentBuilderExtensions}
        ▼
Lus.Application
  Common/Builders/            ← entity-agnostic kernel, ported ~verbatim
  Documents/Builder/
    Orchestration/ Agents/ Commit/ Scaffolding/ Models/ Services/ Entities/ Repositories/
        ▼
Lus.Infrastructure
  Adapters/PythonScriptsWS/PythonScriptsAdapter      ← the ONLY bridge
  SignalRHubs/DocumentBuilderHub
  Services/Workflows/WorkflowProgressService
  Redis provider layer
        ▼
PythonScripts/
  agents/runner.py --agent doc.<name> --payload-stdin
  agents/doc/*.py · agents/schemas/doc.*.json
  render/{xlsx_renderer.py, pdf_renderer.py, template_reader.py}
  pyutil/{model_router, llm_model, credits} · core/{llm, env, jsonio, logging, result}
```

### 4.1 Ported verbatim (namespace rename `ArmyLuz.* → Lus.*` only)

| From | To | LOC |
|---|---|---|
| `Application/Common/Builders/*` | `Lus.Application/Common/Builders/*` | 611 |
| `Infrastructure/Adapters/PythonScriptsWS/PythonScriptsAdapter.RunAgentAsync` + `ProgressEnricherAdapter` | same path under `Lus.Infrastructure` | ~300 of 2790 (only the generic runner; the 16 org-specific spawn sites are NOT ported) |
| `Api/Infrastructure/Extensions/PythonAdapterExtensions.cs` | same | ~60 |
| `PythonScripts/agents/runner.py` | same, registry replaced with `doc.*` | 265 |
| `PythonScripts/pyutil/{model_router, llm_model, credits}.py` | same | ~400 |
| `PythonScripts/core/*` | same | ~200 |
| `Infrastructure/Services/Workflows/WorkflowProgressService` + `Services/Progress/ProgressReporterBase` | same | ~580 |

**The scars that must survive the copy** (each is a fixed production bug; do not "clean up"):

- Payload over **stdin, not argv** — drafts grow with the session; Windows caps a command line at ~32K.
- Static ctor sets `PYTHONUTF8=1` + `PYTHONIOENCODING=utf-8` process-wide — Windows console defaults
  to cp1252 and Hebrew `json.dumps` raises `UnicodeEncodeError`.
- `StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)` — a BOM makes
  every agent fail `invalid_input`.
- `ct.ThrowIfCancellationRequested()` **before** `Process.Start` — on an already-cancelled token
  `Register` fires synchronously, spawning and killing a process that never ran.
- `ct.Register(() => process.Kill(entireProcessTree: true))`.
- The token passed to the stdin write, so a killed child raises `OperationCanceledException` rather
  than a broken-pipe `IOException` that reads like a Python crash.
- Python side: `sys.stdin.reconfigure(encoding="utf-8-sig")` (tolerates a BOM), stdout reconfigured
  to utf-8, `ensure_ascii=False`.

Hebrew/RTL is the happy path in this domain, so none of the above is theoretical.

### 4.2 The envelope law (frozen)

One line of PascalCase JSON on stdout:

```json
{"Ok":true,"Agent":"doc.row_extractor","SchemaVersion":1,"Result":{…},"ErrorInfo":null}
{"Ok":false,"Agent":"…","SchemaVersion":1,"Result":null,
 "ErrorInfo":{"Code":"…","UserMessage":"<he>","UserMessageEn":"<en>"}}
```

- **Exit code is ALWAYS 0 for handled failures.** Non-zero means the interpreter itself died and C#
  raises `PythonScriptException`.
- Tracebacks to **stderr only** — never surfaced to the user.
- Every agent has `agents/schemas/<agent>.result.schema.json`, validated by the runner **before**
  emitting. A violation becomes a safe `schema_validation_failed` envelope, so malformed output can
  never reach the draft.
- Agents are **pure functions**: stdin `{"Draft":…,"Input":…}` → envelope. They never touch the DB.
- `COST:{json}` lines on stderr (`pyutil.credits.emit_cost`) for per-call metering;
  `PROGRESS:{json}` lines on stderr for narration. Stdout stays the pure JSON contract.

### 4.3 The builder anatomy (the concept being moved)

Every builder — and `Documents/` is the first — has the same five parts:

1. **Draft slice** — a versioned draft mutated *only* via patch batches. `DraftPatcher` provides
   apply / undo / redo under an optimistic version guard.
2. **Agents wave** — catalog is the single source of truth; parallel within a wave, sequential across
   the enrichment wave.
3. **Reconciler** — a final sequential pass resolving cross-agent conflicts.
4. **Suggestions** — chip suggestions via provider fan-out.
5. **Commit mapper** — draft → the entity's **canonical MediatR commands**. Builders never bypass
   the command layer.

Session state: **Redis, 7-day TTL, with a DB row rescue** (`DocumentBuildSessionRow`), schema-versioned
so a namespace change self-invalidates stale sessions. `SessionSchemaVersion` may only ever increase.

### 4.4 Agent catalog

Six kinds from the kernel, each a record subtype dispatched by `IBuilderAgentVisitor<T>` (double
dispatch — adding a kind is a compiler-enforced new `Visit` method, never a scattered `switch`):
`Content` · `Validator` · `Planner` · `Advisor` · `Refiner` · `Importer`.

| Agent | Kind | Wave | LLM | Does |
|---|---|---|---|---|
| `doc.router` | Content | — | yes | classify the turn: dictation · correction · question · command · import |
| `doc.template_reader` | Importer | — | no | exemplar `.xlsx` → `DocumentTemplate`; also backfills historical sheets |
| `doc.carry_forward` | Content | 0 | no | clone last month, clear data band, repoint the chain, seed letterhead + account number |
| `doc.schema_planner` | Content | 1 | yes | name each column's semantic meaning + type |
| `doc.row_extractor` | Content | 2 | yes | work-log prose → row patches |
| `doc.formatter` | Content | 3 | no | sum, carry-in, remaining, rate, VAT, total; merge-by-date policy |
| `doc.validator` | Validator | — | no | type/required/duplicate lint + deterministic auto-fix |
| `doc.reviewer` | Content | 4 | yes | final coherence pass before commit |
| `doc.question_planner` | Planner | — | no | pick the single next question |
| `doc.advisor` | Advisor | — | yes | grounded free-text Q&A about the document |

Model tiers via `pyutil/model_router.py` — agents declare a capability tier centrally
(`lite / chat / content / deep`); a deterministic regex/counting score gates escalation; **90–95% of
calls must never reach deep**; budget-aware; parity-when-off (no tier envs ⇒ one legacy model).
Proposed rows: `row_extractor`/`schema_planner`/`reviewer` = `content` (deep-eligible),
`advisor` = `chat`. Deterministic agents get no row.

`doc.formatter` and `doc.carry_forward` are deliberately **LLM-free**. Money and balance figures are
computed, never generated. This is the direct answer to defects 4 and 5.

---

## 4.5 The chat shell (ported from `ArmyLuz.Application/AIChats`)

The builder session (§4.3) is not the chat. ArmyLuz keeps them separate on purpose — *"the chat
shell is genuinely a chat concern"* — and Lus ports that separation: `Lus.Application/Chats/` owns
conversation persistence; `Documents/Builder/` owns the draft. The chat references a builder
session; neither owns the other.

**Ported near-verbatim:**

- **Entities** — `Chat` (was `AiChat`): contact fields, `ChatKind`, `Closed`, `ChatState` status
  machine (NeedMoreInfo → Started → InProgress → Ready → AwaitingApproval →
  UserRequestedChanges…), `FailedAttempts` poison-message guard (≥2 consecutive failures ⇒ message
  flagged and ignored, reset on success), `LanguageType`, messages, org scope. `ChatMessage` (was
  `AiMessage`): sender, IsBot, text, `Editing`/`TempText`, `MessageType`, org scope.
- **The six-kind `MessageType` system** — `Default` · **`Hidden`** (in AI history, never rendered —
  how the FE sends batched refinement commands) · `System` · **`Suggestion`** (interactive card
  with options + hidden AI instruction) · **`SuggestionSelection`** ("You selected: X" bubble
  carrying the merged instruction for replay) · `ToolResult`. This is the concrete mechanism behind
  C9.
- **The suggestions subsystem** (889 LOC) — `IChatSuggestionProvider` fan-out,
  `ChatSuggestionService`, select/dismiss commands with lifecycle states. Providers become
  `NewChatSuggestionProvider` + `DocumentChatSuggestionProvider`.
- **API** — `ChatsController` at `v1/chats`: GetAll · search · delete · `{chatId}/suggestions` ·
  `suggestions/select` · `suggestions/dismiss`. ArmyLuz's org-creation routes do not port.
- **FE** (after plan 07) — `ChatsManagement` module shell, `chat-suggestion-message`,
  `chat-suggestion-selection`, `aiChatService` pair, `ai-chats-table`, and the
  `_ai-chat-rail-shared.scss` mixin that the builder rail (copilot tabs, mention composer) builds on.

**Domain swap:** `AiChatType`'s org values (`CreateOrganization`, `CreatePositions`…) become
`ChatKind.DocumentBuilder` (+ future kinds).

### 4.5.1 The turn-understanding laws (design DNA for `doc.router`)

ArmyLuz's improved chat pipeline (docs: `BUILDER_CHAT_TURN_UNDERSTANDING`,
`ORG_CHAT_UNDERSTANDING_LAYERS`) is ported as **laws**, not code:

1. **Route + Scope.** The router returns the route *and* a scope (entities, action, confidence,
   mentions); the orchestrator runs only in-scope agents. Null or low-confidence scope ⇒ full
   catalog — scope gates, never truncates capability.
2. **No keyword lists in any language anywhere.** Scope is the only gate; the pipeline is
   language-agnostic by construction.
3. **Deterministic identity before adding.** Match → update; ambiguity → question; only true
   strangers are added. (For documents: a dictated row matches an existing subject thread before a
   new one is created.)
4. **Ask, don't guess.** Missing information becomes one planner question with chips.
5. **The summary never lies.** Value-identical patches are dropped, not applied-and-logged; the
   turn summary names real changes only.
6. **No classifier hijack.** No length/shape heuristics deciding "this is an answer" — the router,
   which sees the open question, decides answer-vs-new-instruction.
7. **Derived changes are offered, not imposed** — side-effects surface as suggestion chips.
8. **Lanes must not gate on incidental detail.** ArmyLuz's documented failure: the good extraction
   lane required arrival times, so equivalent phrasings fell into a cruder lane that collapsed
   values with `max()`. For Lus: "3 שעות במשרד" and "מ-9 עד 12 במשרד" land in the same
   `doc.row_extractor` lane.

## 4.6 The Excel import lane (ported discipline + readers)

ArmyLuz's productized import (`POST v1/inquiries/apply/import-excel` → 333 LOC C# handler →
~3.3k LOC Python extract/import with schema-validated intermediates) does not port as domain code —
its **discipline** becomes the C14 contract, and its generic readers become code:

- **Ported code:** `excel_reader.py` + `schema_detector.py` (~200 LOC, generic) as the base of
  `doc.template_reader`; the `roster_import.match_person` deterministic-match-else-ask pattern for
  matching historical rows to existing subjects/instances.
- **Ported shape:** two-phase import — extract to a schema-validated intermediate JSON, then write
  through canonical commands. Never parse-and-write in one step.
- **Ported gates (the import discipline):**
  1. Dry-run first — import previews as draft patches on the canvas before anything commits.
  2. Read-before-write preflight; echo server state byte-for-byte, never assume.
  3. Unclear → skip and report, **never guess**.
  4. Canary → confirm → full run → verify by re-read + full diff; 0 diffs = done.
  5. Idempotent upsert keys — re-import is safe, corrections are resubmission.
  6. Multilingual cell parsing with no hardcoded vocabulary (split `,;|`, slash only between full
     values, canonicalize variants).

## 5. Data model

### 5.1 Entities (`Lus.Application/Documents/`)

```
DocumentSeries          the workbook. Per client-project, open-ended (§2.1).
  Id · OrganizationId · Name · ClientName · TemplateId · ExemplarFileId
  SourceFormat{Xlsx,Xls} · CreatedAt

DocumentTemplate        derived from the exemplar; the draft skeleton.
  Id · SeriesId · Fingerprint · Rtl · ColumnWidths · LetterheadFields · TableHeader
  DataBand{StartRow, Levels, MergePolicy}
  BlockHeight · RepeatPolicy{OnePerSheet, StackedPerSheet}
  TotalsFormulaSet · BillingBlock · DeclarationBlock · ContractBlock?

DocumentInstance        ONE REPORT BLOCK — a whole sheet in archetype 1, one of ~11
                        stacked blocks in archetype 2 (§3.5).
  Id · SeriesId · PeriodStart · PeriodEnd · SheetName · BlockOrdinal · BlockStartRow
  AccountNumber · ProjectName · ContractNumber? · ContractValidFrom? · ContractValidTo?
  CarryInFromInstanceId · Status{Draft,Committed,Rendered}

DocumentDay             the day grouping — archetype 2's merged day-total row.
  Id · InstanceId · Date · DayOfWeek · TotalHours(derived)

DocumentRow             a segment in the data band.
  Id · DayId · Ordinal · StartTime? · EndTime? · Hours · Location · Subject

DocumentBuildSession    the live builder session (Redis + this DB rescue row).
  Id · UserId · InstanceId · SchemaVersion · Version · DraftJson · UpdatedAt

Chat                    the conversation shell (§4.5, ported from AiChat).
  Id · OrganizationId · Kind{DocumentBuilder,…} · Status(ChatState) · Closed
  FailedAttempts · Language · BuildSessionId? · CreatedAt

ChatMessage             one message (ported from AiMessage).
  Id · ChatId · Sender · IsBot · Text · MessageType{Default,Hidden,System,
  Suggestion,SuggestionSelection,ToolResult} · OrganizationId · CreatedAt

RateCard                rates out of the stale sheet and into storage.
  Id · SeriesId · EffectiveFrom · HourlyRate · VatPercent · PlotsPercent?
```

**Derived, never asked and never typed:** `DocumentDay.DayOfWeek` (from `Date`),
`DocumentDay.TotalHours` (sum of its segments), `DocumentRow.Hours` (from `EndTime − StartTime`
when both are present).

The two-level `DocumentDay` → `DocumentRow` split is what lets one model serve both archetypes:
archetype 1 is simply the degenerate case of exactly one segment per day with no times.

### 5.2 Draft & patch ops

The draft is the JSON projection of one `DocumentInstance` + its rows + resolved template. Agents
never return a whole draft — they return **patch ops**:

```
{ "Op": "AddRow"|"UpdateRow"|"RemoveRow"|"SetField"|"SetTotals",
  "Path": "rows[3].hours" | "letterhead.accountNumber" | …,
  "Value": … }
```

`DraftPatcher` applies a batch atomically under an optimistic `Version` guard, and records the
inverse batch so undo/redo is free. **Patches are the only mutation path** — no direct writes.

---

## 6. One turn, end to end

1. User types into the chat rail (or uploads a file).
2. `POST v1/documents/builder/turn` with `{sessionId, version, text|fileId}`.
3. `DocumentBuilderOrchestrator` classifies the turn (dictation / question / correction / command).
4. Runs the wave. Each agent → `BuilderAgentClient` → `PythonScriptsAdapter.RunAgentAsync` →
   `runner.py --agent doc.<name>`.
5. Each agent returns patch ops → `DraftPatcher` applies them under the version guard.
6. Each applied batch fires `DraftPatched` over SignalR → the canvas animates the changed cells.
   `AgentStatus` drives the ticker ("doc.row_extractor is thinking…").
7. `doc.question_planner` emits the next question as chips (`QuestionAsked`).
8. Session persisted to Redis (7-day TTL) + DB rescue row.
9. Response returns the new version + ops (so the turn works with SignalR down).

### 6.1 Commit

`POST v1/documents/builder/commit` → `doc.validator` hard pass → `DocumentCommitService` maps the
draft into canonical MediatR commands (`CreateDocumentInstanceCommand`, `UpsertDocumentRowsCommand`)
→ renderer.

**Renderer (`render/xlsx_renderer.py`)**: reopen the **exemplar workbook** with `openpyxl`, clone the
template sheet, name it for the period, write the data band, apply the merge-by-date policy, write
the totals/billing formulas, repoint the carry-in reference to the previous instance's actual
remaining-cell. Styles, merges, widths, print setup are **inherited, never rebuilt**.

**PDF (`render/pdf_renderer.py`)**: same draft, page-set renderer. The exemplar PDFs are the print of
the sheet, so PDF is a rendering target, not a second design.

---

## 7. Error handling

Three independent fences, each already proven in ArmyLuz:

1. **Python never exits non-zero for a handled failure.** Safe bilingual envelope; traceback to
   stderr only.
2. **Schema validation before emit.** Malformed agent output cannot reach the draft.
3. **A failed agent fails its wave slot, not the turn.** The orchestrator records the failure,
   continues the wave, and reports it in the turn result.

SignalR is **enhancement-only**: if the hub is down the turn still completes over the HTTP response;
the user loses the ticker, not the work. Progress emission is fire-and-forget and may never fail an
operation.

---

## 8. Loading UX (bundled — the builder needs tier 3)

Delete `Adapters/loader` + `blockUI-request` (the full-screen blocking overlay ArmyLuz removed).
Port the three tiers:

| Tier | When | Surface |
|---|---|---|
| 1 Global activity | any tracked HTTP request | mini-toast, pulsing icon + what is loading |
| 2 Region loading | a screen fetching its data | `app-skeleton` shimmer |
| 3 Narrated long op | the builder wave | SignalR `WorkflowProgress` → stage text + honest percent |

Laws: nothing blocks the whole screen · **fast is invisible** (<300ms shows nothing; appears after
300ms continuous, stays ≥400ms) · **counter, not boolean** (`begin()/end()` paired in the
interceptor's `finalize`; cannot leak or go negative) · `LoadingService` never injects anything
HTTP-adjacent (circular-DI law) · `bypassSpinner` HttpContext token for flows with their own surface ·
narration is enhancement-only · percentages honest only where a real denominator exists · FE owns
narration copy, BE sends stage keys.

This is not optional scope: a multi-agent wave behind a blocking overlay is unusable.

---

## 9. Auth hardening (D4)

Port from `ArmyLuz.Authorization`: Permissions RBAC (`PermissionAttribute` +
`PermissionPolicyProvider` + `PermissionRequirementHandler` + `PermissionRuleDefinition`),
`SecurityStampValidator` + `SecurityStampStore`, `IAuthStateService`/`IAuthStateResolver`,
`ReauthTokenService`, `SecurityHeadersMiddleware`, `CorrelationIdMiddleware`,
`Adapters/AppleSignin`, `PersistentSecurityAuditService`.

**Not ported**: `TenantResolutionMiddleware`, `SubdomainRedirectService`, choose-organization,
apex-only routing. Lus keeps per-handler org scoping unchanged.

First permission: `[Permission("documents.build")]` on `DocumentBuilderController`.

---

## 10. Testing

### 10.1 Guard tests (ported — they are what stops the layers re-tangling)

- `CommonBuildersKernel_ReferencesNoEntitySpecificTypes` — reflection over every kernel type's bases,
  interfaces, members and params; no `Documents` type may leak into `Common/Builders`.
- `DocumentsBuilder_DependsOnlyOnTheKernel`.
- `SessionSchemaVersion_OnlyEverIncreases`.
- `DocumentBuilderControllerRouteTests` — the route set is an explicit decision.
- `test_runner_aliases.py` — every alias targets a registered agent; alias↔package law
  (`doc.*` ⇒ module in `agents/doc/`); subprocess parity; unknown agent still yields a safe envelope.
- `test_model_router.py` — every LLM-bearing agent in the registry has a tier row (hand-synced lists
  drift).

### 10.2 Domain tests

- **Golden-file render**: exemplar in → filled workbook out → assert styles, merges, column widths,
  RTL and formula strings byte-identical to a committed golden.
- **Balance chain**: a three-month series; assert `E(n) = D(n) − C(n)` and `D(n+1) = E(n)` with the
  carry-in reference pointing at the *actual* remaining-cell row, whatever the band length.
- **Sheet-name tolerance**: a series whose exemplar has trailing/leading/double-space sheet names
  still resolves its chain (defect 2).
- **Zero-total lint**: a draft with an empty rate must fail `doc.validator` and block commit
  (defect 5 — the live money bug).
- **Hebrew round-trip**: Hebrew subject text survives C# → stdin → Python → stdout → draft → render
  unmangled, with and without a BOM-emitting writer.
- **Derived day-of-week**: `Date` → `יום השבוע` never asked, never typed.

---

## 11. Phases

| P | Scope | Exit criterion |
|---|---|---|
| **P0** | Redis + Python runtime in the Lus Docker image; Railway config. (SignalR already exists — a new hub is added, not the framework.) | `docker compose up` serves the API with a reachable hub and `python3 -c "import openpyxl"` inside the image |
| **P1** | Kernel + `PythonScriptsAdapter` + `runner.py` + `model_router`/`credits`/`core` | A trivial `doc.echo` agent round-trips Hebrew text end-to-end from a controller |
| **PA** | Angular 18 → 20 (D8), `@microsoft/signalr` added. Independent; must land before any FE port work | `ng build` + `ng test` green on 20; app runs unchanged |
| **P2** | Loader replacement (tiers 1–3), delete the blocking overlay | No full-screen block anywhere; <300ms requests show nothing |
| **P3** | `Documents/Builder/` backend: entities, draft model, `DraftPatcher`, session store, orchestrator, catalog, turn endpoint — **plus the chat shell** (§4.5: Chat/ChatMessage entities, message-type system, suggestions subsystem, `v1/chats`) | A turn applies patches and bumps the version, with undo/redo; messages persist with the six-kind type system |
| **P4** | The doc agents + `doc.template_reader` on the real exemplar | The supplied workbook parses into a `DocumentTemplate` with all five blocks identified |
| **P5** | FE: chat rail, agent ticker, question chips, doc-canvas preview | Dictating a work-log line renders a new row live on the canvas |
| **P6** | Commit + openpyxl round-trip `.xlsx` renderer | Golden-file test green against the supplied workbook |
| **P7** | PDF renderer | Output matches the supplied monthly PDFs |
| **P8** | Auth hardening | `[Permission("documents.build")]` enforced; security stamp invalidation works |

Each phase gets its own plan file in `.brain/`.

---

## 12. Reference docs to be written

| Doc | Content |
|---|---|
| `docs/BUILDERS_ARCHITECTURE.md` | The kernel, the anatomy, the laws, the guard tests, the add-a-builder checklist |
| `docs/DOCUMENT_BUILDER.md` | The document model, template derivation, balance chain, renderers |
| `docs/PYTHON_AGENTS_BRIDGE.md` | The subprocess contract, envelope law, the scars, model routing, metering |
| `docs/NON_BLOCKING_LOADING.md` | The three tiers and their laws |
| `docs/AUTH_HARDENING.md` | What was ported, what was deliberately not, and why |

---

## 12.1 The end state — smart concepts

The capability set this design builds toward is enumerated concept by concept in
**[`docs/DOCUMENT_BUILDER_SMART_CONCEPTS.md`](../../DOCUMENT_BUILDER_SMART_CONCEPTS.md)** — sixteen
concepts (C1–C16), each with its mechanism, the agents that deliver it, the failure it prevents, and
the phase it lands in.

The three that most shape the architecture:

- **C3 Editable canvas** — a human clicking a cell emits the *same patch op* an agent emits. There is
  no second write path, so undo, redo, validation and the version guard work identically for both.
  Derived cells are not editable as values; clicking one explains it instead (C10).
- **C6 Derivation engine** — six of the sixteen concepts are deliberately **LLM-free**. Everything
  that touches money is arithmetic, sourced from the `RateCard` entity.
- **C7 Series brain** — the carry-in is an entity relationship (`CarryInFromInstanceId`), rendered
  down to a cell reference only at write time. That is what makes exemplar defects 1–3 structurally
  impossible rather than merely discouraged.

## 13. Risks

| Risk | Mitigation |
|---|---|
| The org builder's 9507 LOC tempts a wholesale copy, dragging scheduling domain into Lus | Only `Common/Builders` (611 LOC) is ported. `Documents/Builder/` is written fresh against the kernel. Enforced by the kernel guard test. |
| Exemplar fidelity is judged by eye and regresses silently | Golden-file render tests with byte-level style comparison, committed against the supplied workbook. |
| P0 is unglamorous infra and gets skipped or half-done | It is a gated phase with an explicit exit criterion; nothing in P1+ runs without it. |
| Hebrew/RTL mangling | The ported scars, plus an explicit round-trip test including the BOM case. |
| LLM invents billing numbers | `doc.formatter` and `doc.carry_forward` are LLM-free by design; the validator blocks commit on an empty rate. |
| Python process spawn cost per agent call | Accepted (D1). `IAgentTransport` is *not* pre-abstracted (ARCH-1: extract bottom-up); if it bites, the adapter is one seam to swap. |
