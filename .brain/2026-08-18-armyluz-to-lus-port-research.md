# Research — What ArmyLuz has, what Lus needs (port inventory)

> Date: 2026-08-18. Research only — no design decisions committed here.
> Sources: `/Users/onecity/Desktop/projects/ArmyLuz` (223 docs, `PythonScripts/`, `ArmyLuz.UI/`),
> this repo (`src/`, `docs/`, `.brain/`).

## 0. The relationship between the two codebases

Lus and ArmyLuz are **the same architecture, different maturity**. Identical project split
(`*.Api / *.Application / *.Infrastructure / *.Authorization / *.Contracts / *.NotificationCenter /
*.FilterEngine / *.UI`), identical Angular folder convention (`Activities / Adapters /
Infrastructure`), identical CQRS-lite MediatR + repository + projections + AutoMapper patterns.
Lus's own `docs/ARCHITECTURE.md` already says FilterEngine was "ported from ArmyLuz".

**Consequence: this is a port, not a rewrite.** Namespaces rename `ArmyLuz.* → Lus.*` and most
files compile. The risk is not "will it fit" — it is "how much of the org/scheduling *domain*
comes along by accident".

| | ArmyLuz | Lus |
|---|---|---|
| Domain | Workforce scheduling (orgs, shifts, persons, rules, assignments) | Projects + project times + templates → **Excel export** |
| Api controllers | ~40 | 8 |
| Application slices | 30 | 11 |
| Infra services | 20 dirs incl. Workflows, Progress, SignalRHubs, Redis, Billing, AiFlows | 2 files |
| Adapters | AppleSignin, GoogleSignin, **PythonScriptsWS**, Recaptcha, SystemWS | Google, Recaptcha, SystemWS |
| Authorization | 2055 LOC — + Permissions RBAC, middleware, AuthState, Reauth, SecurityStamp, Audit | 784 LOC — cookie + CSRF + UserAccessor only |
| UI Activities | 26 areas | 9 screens |
| UI services | 55 | 8 |
| Python | `PythonScripts/` — 29-agent runtime, ~26k LOC in `agents/` alone | none |
| Realtime | SignalR hubs + workflow progress | none (Hangfire present, no SignalR) |

---

## 1. The AI / agents subsystem (the main thing to move)

### 1.1 The five-layer stack

```
Angular  builder-state.service ──HTTP POST v1/organizations/builder/turn
                               └─SignalR hub/organization-creation (live canvas events)
   │
C# API   OrgBuilderController
   │
C# App   <Entity>/Builder/Orchestration/<X>BuilderOrchestrator   ← the turn brain
         <Entity>/Builder/Agents/<X>BuilderAgentClient
         Common/Builders/*                                        ← entity-agnostic kernel
   │
C# Infra Adapters/PythonScriptsWS/PythonScriptsAdapter.RunAgentAsync   ← the ONLY bridge
   │
Python   PythonScripts/agents/runner.py --agent <name> --payload-stdin
         → agents/{org,rules,generate}/<agent>.py  → llm_providers → OpenAI/Claude
```

### 1.2 The process bridge — `PythonScriptsAdapter` (2790 LOC, 17 spawn sites)

`RunAgentAsync(agentName, draftJson, inputJson, langCode, ct)`:

- `ProcessStartInfo` → `pythonExePath agents/runner.py --agent X --lang he --non-interactive
  --payload-stdin [--api-key K]`, `WorkingDirectory = scriptsPath`.
- **Payload over STDIN, not argv** — drafts grow with the session and Windows caps a command line
  at ~32K chars. One JSON doc: `{"Draft": …, "Input": …}`.
- **UTF-8 everywhere, BOM-less stdin.** Static ctor sets `PYTHONUTF8=1` + `PYTHONIOENCODING=utf-8`
  process-wide (Windows console defaults to cp1252 → Hebrew `json.dumps` crashes). `StandardInput
  Encoding = new UTF8Encoding(false)` — a BOM makes every agent fail `invalid_input`.
- **Cancellation**: `ct.ThrowIfCancellationRequested()` *before* `Process.Start` (an already-cancelled
  token makes `Register` fire synchronously and kill a process that never ran), then
  `ct.Register(() => process.Kill(entireProcessTree: true))`, and the token is passed to the stdin
  write so a killed child raises `OperationCanceledException`, not a broken-pipe `IOException`.
- Wired in `ArmyLuz.Api/Infrastructure/Extensions/PythonAdapterExtensions.cs` from config
  (scriptsPath, pythonExePath, apiKey).

### 1.3 The envelope law (`runner.py`, 265 LOC)

One line of PascalCase JSON on stdout:
`{"Ok":true,"Agent":…,"SchemaVersion":1,"Result":{…},"ErrorInfo":null}` — or `Ok:false` with
`ErrorInfo{Code, UserMessage(he), UserMessageEn}`.
**Exit code is ALWAYS 0 for handled failures**; non-zero means the interpreter itself died and C#
raises `PythonScriptException`. Tracebacks go to stderr only — never to the user.
Each agent has a JSON Schema at `agents/schemas/<agent>.result.schema.json`, validated by the runner
*before* emitting; a violation becomes a safe `schema_validation_failed` envelope.
Agent names are namespaced (`org.roles`, `rules.text`) with a back-compat alias table to flat names.

### 1.4 The agent catalog (`Common/Builders/IBuilderAgentCatalog.cs`)

Six kinds, each a record subtype with a **double-dispatch visitor** (`Accept<T>`) so invocation
strategy lives in one place and adding a kind is compiler-enforced:

| Kind | Meaning | Examples |
|---|---|---|
| `Content` | emits draft patches, wave-ordered | roles, crews, shifts, people, rules |
| `Validator` | coherence lint + deterministic auto-fix | validator_agent |
| `Planner` | picks the single next interview question | question_planner |
| `Advisor` | grounded free-text advice (copilot mode) | advisor_agent |
| `Refiner` | sharpens a raw request before it runs | prompt_refiner |
| `Importer` | **ingests an uploaded file into draft patches (Excel/CSV)** | ← the hook for Lus |

Waves: 1 `roles, crews` → 2 `shifts, people` → 3 `rules` → 4-9 sequential rules enrichment
(`rules_text → slots_demand → day_variant → crew_rules → rule_dependency → rules_reconciler`).
Parallel *within* a wave, strictly sequential *across* the enrichment wave.

### 1.5 The reusable kernel already extracted (`Application/Common/Builders/`, 611 LOC)

`AgentResult<T>` · `BuilderAgentClientCore` · `SequentialAgentWaveRunner` · `BuilderTurnContext` ·
`IBuilderEventSender<TPatchOp,TQuestion,TWarning>` · `IBuilderAgentCatalog` + descriptor hierarchy ·
`BuilderSessionStoreBase`. Law ARCH-1: kernel types are extracted **bottom-up** once a second
builder needs them, never pre-generalized. Pinned by `BuilderArchitectureGuardTests` (reflection
over every kernel type — no entity type may leak in).

### 1.6 The repeatable builder anatomy (`docs/BUILDERS_ARCHITECTURE.md`)

1. **Draft slice** — versioned draft, mutated only via patch batches (`DraftPatcher`: apply/undo/redo,
   optimistic version guard). Org's is 798 LOC.
2. **Agents wave** — pure functions, never touch the DB.
3. **Reconciler** — final sequential pass resolving cross-agent conflicts.
4. **Suggestions** — chip suggestions via provider fan-out.
5. **Commit mapper** — draft → the entity's **canonical MediatR commands**. Builders never bypass
   the command layer.

Session state: Redis 7-day TTL + a DB row rescue (`OrgBuildSessionRow`), schema-versioned so a
namespace move self-invalidates stale sessions.

Sizes to expect if the org builder came over wholesale: `OrgBuilderOrchestrator` 2893 LOC,
`OrgBuilderCommitService` 1317, `AnswerMerger` 1018, `DraftPatcher` 798, `DraftToResourcesMapper` 667.
**Almost all of it is scheduling-domain, not kernel.** The transferable part is ~600 LOC of kernel +
the adapter + the runner + the FE canvas/chat shell.

### 1.7 Python side

- `agents/` — 29 agents, ~26k LOC, `llm_providers/{openai,claude,factory,base}`, `schemas/`,
  `knowledge/benchmarks/*.json`, `templates/questions.json`.
- `pyutil/model_router.py` — **the one selection layer**. Agents declare a capability *tier*
  (`lite / chat / content / deep`) centrally; model ids come from env set by C# from appsettings.
  Deterministic regex/counting complexity score gates escalation; "90-95% of calls must never reach
  deep"; budget-aware (`AIB_BUDGET_STATE=warning|critical`); one-tier ladder retry on empty answers;
  parity-when-off (no tier envs → legacy single model).
- `pyutil/credits.py` — `emit_cost()` writes `COST:{json}` to **stderr**; C#'s `MeteredPythonExecutor`
  parses it for per-call billing. Stdout stays the pure JSON contract.
- `core/{llm,env,jsonio,logging,result}.py` — the kernel every script shares.
- Progress: `PROGRESS:{json}` lines on stderr → C# → SignalR.
- Deps: `openai`, `langchain`, `jsonschema`, `pandas`, **`openpyxl`**, `redis`, `pytest`.
- There is also a **second, newer Python layout** — `ArmyLuz.Ai.PythonEngine/` — a FastAPI service
  (`src/pythonproject/api/main.py`, routes, `task_router`, DTO contracts mirroring C# Contracts,
  and an `application/services/excel/` package: `excel_reader`, `schema_detector`, `crew_extractor`).
  This is the "future FastAPI service" the `pyproject.toml` description mentions. **Two lanes exist;
  the subprocess lane is the one actually wired to C#.**

### 1.8 Excel / PDF today

- **Excel read (AI)**: python `pandas` + `openpyxl`; `excel/schema_detector.py` + `excel_reader.py` +
  `crew_extractor.py`; `Application/Inquiries/Commands/ImportAvailabilityWorkbook`; docs
  `EXCEL_CREW_IMPORT_RULES.md`, `AVAILABILITY_EXCEL_INQUIRIES_IMPORT.md`, `WHITEBOARD_AI_IMPORT.md`.
- **Excel write**: only in the **frontend** — Lus already does this today with `xlsx-js-style` in
  `excel-export.component.ts` (styled borders, merged cells, signature rows).
- **PDF: nothing exists in either project.** No QuestPDF/iText/reportlab/jsPDF anywhere. This is
  net-new work, not a port.

---

## 2. Login / auth flows

Lus has cookie auth + CSRF + `UserAccessor` and stops there. ArmyLuz adds, in
`ArmyLuz.Authorization/`:

- **Permissions RBAC** — `PermissionAttribute` + `PermissionPolicyProvider` + `PermissionRequirement
  Handler` + `PermissionRuleDefinition` (declarative `[Permission("x.y")]` on controllers).
- **Middleware** — `TenantResolutionMiddleware` (subdomain → org), `CorrelationIdMiddleware`,
  `SecurityHeadersMiddleware`.
- **Session hardening** — `SecurityStampValidator` (server-side invalidation on password/role change),
  `SecurityStampStore`, `PersistentSecurityAuditService`, `UserLoginAttemptsAuditService` (Lus has
  this one already).
- **Reauth** — `ReauthTokenService` (short-lived step-up token for sensitive actions).
- **AuthState** — `IAuthStateService`/`IAuthStateResolver`: one endpoint the FE asks "who am I, what
  can I do, where do I land".
- **Social** — `Adapters/GoogleSignin` (Lus has Google) + `Adapters/AppleSignin` (Lus lacks).

Flow doctrine (`docs/WEEKEYE_AUTH_FLOWS.md`, `AUTH_MIGRATION_PLAN.md`, `POST_LOGIN_DIRECT_LANDING.md`,
`IOS_FACEID_GOOGLE_SIGNIN_FLOW.md`):

1. **Apex-only auth** — every `/authentication-*` route on the apex domain; org subdomains never host
   a login page.
2. **Post-login redirect** — `SubdomainRedirectService`: single org → `acme.host/dashboard`;
   multiple → `/choose-organization` → pick → redirect.
3. **Biometric is opt-in, post-login only** — never prompt on fresh install; the enrollment offer
   modal appears after the first successful login on the device.
4. **Password / Google / Apple always available** — biometric can never be the only method.
5. Six languages (HE, EN, AR, ES, FR, RU); Apple sign-in behind `environment.appleSignInEnabled`.

---

## 3. Loader / loading UX

Lus today: `Adapters/loader` + `blockUI-request` interceptor = the **legacy blocking overlay**
ArmyLuz explicitly deleted.

ArmyLuz replacement (`docs/NON_BLOCKING_LOADING.md`, `AI_GENERATE_LOGO_LOADER.md`) — three tiers:

| Tier | When | Surface |
|---|---|---|
| 1 Global activity | any tracked HTTP request | mini-toast, pulsing icon + "what is loading"; **hysteresis: appears after 300ms, stays ≥400ms** |
| 2 Region loading | a screen fetching its data | `app-skeleton` shimmer |
| 3 Narrated long op | multi-second server work | SignalR `WorkflowProgress` → stage text + honest percent in the flow's own overlay / progress toast |

Laws: nothing blocks the whole screen · fast is invisible (<300ms shows nothing) · **counter, not
boolean** (`begin()/end()` paired in the interceptor's `finalize`, cannot leak or go negative) ·
`bypassSpinner` HttpContext token for flows with their own surface · narration is enhancement-only
(SignalR down ⇒ operation still completes) · percentages honest only where a real denominator exists ·
FE owns narration copy, BE sends stage keys.
Plus the branded loader: theme-aware GIF/MP4, background erased with `mix-blend-mode` — and the
critical detail that the scrim + `backdrop-filter` must live on `::before`, not the overlay div.

---

## 4. Organization logic

ArmyLuz `Organizations/` is far past Lus's: subdomain service (+ `LLM_SmartSubdomain.py`),
`OrganizationResourcesManager`, `OrganizationCacheService`, `OrganizationPreferences`,
type/subtype system (`TYPE_AWARE_ORGANIZATION_SYSTEM.md`, archetype template provider, benchmark
knowledge packs), grant/owner hierarchy (`GRANT_ORGANIZATION_HIERARCHY_ARCHITECTURE.md`),
regional i18n, holidays, delete lifecycle, `OrganizationCreationJob` + progress reporter + stage
catalog. Lus has `Application/Organizations` + `Contracts/Organizations` and per-handler org scoping
(`docs/ORG_PROJECTIONS_SEARCH.md`), no tenant middleware.

---

## 5. Frontend canvas + chat (the shape Lus wants)

`Activities/OrganizationsManagement/`:
- `ai-org-builder/` — the shell; `org-canvas/` = 12 live canvas widgets (graph, heatmap, coverage,
  KPI header, persons, crews, positions grid, rules list, assignments, **agent ticker**, count-up
  directive); editors as side drawers (`builder-rule-editor`, `builder-person-editor`,
  `builder-shift-editor`, `builder-assignment-editor`, `builder-node-editor`); `builder-question-chips`.
- `ai-rules-builder/` + `rules-canvas/` — the second builder, proving the pattern repeats.
- `ai-builder-tabs/` — switches between them.
- Services: `organizationBuilderService/{ai-builder-api, builder-state, builder-payload.normalizer,
  builder.types, text-direction.util}` and the rules twin.
- `ChatsManagement/` — chat shell, suggestion message/selection components; `ai-chat-rail-core` SCSS
  mixin shared by both builders; copilot mode tabs (build / advisor); mention-style composer.

FE laws worth carrying: canvas custom element must set `:host{display:flex;flex:1;min-height:0}`;
one editor component reused by both builders; every draft edit goes through **one** `canvasCommand`
turn, never a direct write.

---

## 6. What actually needs to come to Lus (grouped)

**A. Kernel / plumbing (port near-verbatim, rename namespaces)**
`Common/Builders/*` (611 LOC) · `PythonScriptsAdapter.RunAgentAsync` + `PythonAdapterExtensions` ·
`agents/runner.py` + envelope + schema-per-agent + alias registry · `pyutil/{model_router,
llm_model,credits}` + `core/*` · SignalR `WorkflowHubBase` + `IWorkflowProgressService` +
`ProgressReporterBase` + stage catalog · Redis provider layer (Lus is EasyCaching-in-memory today).

**B. Loading UX (port + delete the old)**
`LoadingService` (counter) · mini-toast · `app-skeleton` · `bypassSpinner` token ·
`ProgressReporterRegistry`. **Delete** `Adapters/loader` + `blockUI-request`.

**C. Auth (port selectively)**
Permissions RBAC · SecurityStampValidator · AuthState endpoint · Reauth · security headers +
correlation middleware · Apple sign-in adapter · apex-only + post-login-landing doctrine.
Tenant middleware only if Lus goes subdomain-per-org.

**D. FE builder shell (port the shape, not the content)**
`builder-state.service` + `ai-builder-api.service` + normalizer + types · chat rail core mixin +
suggestion components · agent ticker · question chips · canvas host law. The 12 org canvas widgets
are scheduling-specific — Lus's canvas is a **spreadsheet/document preview** instead.

**E. Net-new for Lus (no source to port)**
A **Document builder** entity: draft = the workbook/document model; agents = document agents
(schema planner, column mapper, row extractor, formatter, validator, reviewer); commit = render to
`.xlsx` / `.pdf`. PDF generation has no precedent in either repo. The `Importer` agent kind + the
python `excel/{excel_reader, schema_detector}` services are the closest existing seeds.

---

## 7. Open questions blocking design

1. **Python transport** — copy the proven subprocess lane (`PythonScriptsAdapter` + `runner.py`), or
   adopt the newer `ArmyLuz.Ai.PythonEngine` FastAPI service (cleaner, but not the lane that is
   battle-tested and wired)?
2. **Document model** — is the Excel/PDF draft one generic "tabular document" model with a renderer
   per format, or two separate builders?
3. **Does Lus become multi-tenant/subdomain** (drives tenant middleware, apex-only auth, org logic
   depth) or stay single-org-per-user?
4. **Redis + SignalR** — Lus has neither. Live canvas + narrated progress + session store all assume
   them. Add both, or degrade (DB-only sessions, polling)?
5. **Scope order** — does the AI/agent plumbing land first (user's stated ask), with loader/auth/org
   as follow-on phases?

---

## 8. Clarifications received during research (2026-08-18)

- **Move the builder *concepts*, not just the plumbing.** The whole repeatable anatomy comes over —
  draft slice + patch batches, agent catalog + waves, reconciler, suggestions, commit-through-
  canonical-commands, session store, event contract — as `Lus.Application/Common/Builders/` plus a
  first concrete `<Entity>/Builder/`. §1.5 + §1.6 are in scope, not just §1.2/§1.3.
- **Today's Lus Excel flow is the pain being replaced.** `excel-export.component.ts` builds the
  workbook cell-by-cell in the browser (`xlsx-js-style`, hand-rolled border/merge/signature logic),
  and everything that feeds it is typed by hand through `create-project` / `data-loader` /
  `project-validator`. It is slow, manual, and per-project. The chat+canvas builder replaces the
  *data entry*, and the commit stage replaces the *hand-rolled writer* — the conversation fills the
  document draft, the renderer emits `.xlsx` or `.pdf`.

---

## 9. Exemplar analysis — `/Users/onecity/Downloads/attachments` (2026-08-18)

Four real artifacts: one master workbook + three monthly PDFs. **These are the Lus domain**, and
they change the document-builder design from speculative to specified.

### 9.1 What the document is

A monthly **work-hours performance report / account** (`דוח ביצוע שעות עבודה`) issued by a
one-person traffic-engineering practice (`אהובה אליה — הנדסת תנועה ותחבורה`) to a client
(`רשות שדות התעופה` / Israel Airports Authority), via `תדם`. Each month's sheet is printed to PDF
and becomes the signed invoice (`ח-ן`), filename pattern `01032601 ר.ש.ת מרץ 2026 ח-ן.pdf`
(= `DDMMYY01` account number + client + month + year).

### 9.2 Workbook shape

`חשבון שעות לפרויקטים ר.ש.ת 2026-אא.xlsx` — 19 sheets, all `rightToLeft=True`:
`תעריפים` (rates/master template) + one sheet per month `דצמבר 2024 … אפריל 2026` + an empty `גיליון2`.

Every month sheet is the same five-block template:

| Block | Rows | Content |
|---|---|---|
| Title | 2, 4 | client name; `דוח ביצוע שעות עבודה <month year>` |
| Letterhead | 5–7 | practice name, address, phone, VAT id; `לקוח`/`תדם`/date; `המתכנן`; `מספר ח-ן` |
| Table header | 10 | `תאריך │ יום השבוע │ סהכ שעות עבודה │ מיקום │ נושא העבודה` |
| **Data band** | 11 → N | one row per work item; **A and B merged vertically** across all rows sharing a date |
| Totals | N+1 | `סה"כ` · `=SUM(C…)` · carry-in · remaining |
| Billing | +3…+7 | total hours · rate · subtotal · VAT 18% · total |
| Declaration | +9…+16 | employee declaration text, signatures, planner + municipality approval |

Column widths are fixed (`A=25.6, B=20.6, D=15.6, E=77.5, F=8.6`); ~31–45 merged ranges per sheet.

### 9.3 The balance mechanic (the non-obvious part)

The totals row is a **contract-hours burn-down**, not a simple sum:

```
C = SUM(data band)              hours consumed this month
D = ='<previous month sheet>'!E<row>   balance carried in
E = D - C                       balance remaining
```
March 2026: `C25=SUM(C11:C24)=32`, `D25=+'פברואר  2026 '!E31 = 760`, `E25 = 728`.

So each sheet is chained to the previous one by a hand-typed cross-sheet reference whose **row number
differs every month** (E14, E22, E31, E45 …) because the data band length varies.

### 9.4 Everything wrong with the manual process (the actual value case)

Read straight off the file — this is what "old fashion, slow, insert all" means in practice:

1. **Hand-typed cross-sheet refs.** `='פברואר  2026 '!E31` must match a sheet name *and* a row that
   moves monthly. One wrong row silently corrupts the balance chain.
2. **Sheet names are inconsistent whitespace.** `'מרץ 2025 '`, `'אפריל 2025  '`, `' אפריל 2026 '`,
   `'פברואר  2026 '` — leading/trailing/double spaces, hand-typed, and formulas must match byte-exact.
3. **Layout drift between years.** 2025 sheets put the billing block in column C; 2026 sheets moved it
   to column D. The template was copied and edited by hand, so it diverged.
4. **Duplicated values that should be formulas.** March 2026 `D28=32` is typed by hand while
   `C25=SUM(C11:C24)` computes the same 32.
5. **The billing block is BROKEN in production.** In both Feb and Mar 2026: `מחיר לשעת עבודה` empty,
   `סה"כ לחשבון` empty, `מע"מ 18%` empty — and `D32=D31+D30` therefore renders **`0.00`** on the
   issued PDF. The rate (`225`) exists only on the stale `תעריפים` sheet. **They are shipping
   zero-total invoices.**
6. **`תעריפים` is a stale Jan-2025 copy** doing double duty as the master template and the rate store.

### 9.5 What this pins down in the design

- **Exemplar → template is confirmed as the right primitive**, and the template is richer than
  "columns + styles": it is `{letterhead fields, table header, data band start/end, merge policy
  (group by date), totals formula set, billing block, declaration block, RTL, column widths}`.
- **A document is a *series*, not a single file.** The builder's unit is the workbook; a new month
  clones the template, clears the data band, and **repoints the carry-in reference automatically** —
  killing failure modes 1, 2, and 3.
- **Derived values must be computed, never typed** — sum, carry-in, remaining, rate, VAT, total.
  Failure modes 4 and 5 are exactly what the `doc.formatter` + `doc.validator` agents exist to
  prevent, and a "no empty billing block / no zero total" lint would have caught a live money bug.
- **Rates belong in structured storage**, not a stale sheet — Lus already has `ProjectTemplate`
  entities to hold them.
- **The chat turn is naturally phrased as work-log dictation**: "5 במרץ, 3 שעות במשרד, התייעצות
  מחדש עם מתכנן התנועה…" → `doc.row_extractor` → row patches, with the day-of-week (`ה`) derived,
  not asked.
- **Hebrew/RTL is load-bearing everywhere** — the ported UTF-8/BOM/`PYTHONUTF8` scars in
  `PythonScriptsAdapter` are not theoretical here, they are the happy path.
- **PDF is the print of the sheet**, not a separate design. Same draft, page-set renderer — which
  supports the one-doc-model/two-renderers decision.

---

## 10. Second exemplar batch — `/Users/onecity/Downloads/attachments (1)` (2026-08-18)

Four files: the ר.ש.ת hours workbook again **plus a `מקור` twin**, and a second, richer document
type (`אומדן פרוייקטים חדש שוטף-2026.xls`) **also with a `מקור` twin**.

### 10.1 `מקור` means "manual backup", not "template"

A full cell-by-cell diff of `חשבון שעות … מקור … .xlsx` against the working copy returns
**0 differing cells** across all 19 sheets. The `.xls` pair differs by 512 bytes of container
padding.

So `מקור` is not a blank master — it is **version control by filename copy**, taken before editing.
Add it to the defect list (§9.4): there is no history, no diff, and no way to tell which copy is
authoritative beyond the filename.

The Document Builder replaces this with real instance versioning — `DocumentInstance.Status` plus
the draft's monotonic `Version` and inverse patch batches (undo/redo).

### 10.2 Archetype 2 — the municipal project estimate register

`אומדן פרוייקטים חדש שוטף-2026.xls` — legacy BIFF `.xls`, **35 sheets**, ~450 rows each, one sheet
per month (Jul 2022 → Jun 2026) plus an `old` sheet.

Client: `עירית תל אביב`, `אגף דרכים ומאור- מחלקת דרכים (01)`. Same practice, same declaration block,
same balance mechanic — but structurally a different document.

**The defining difference: many report blocks stacked vertically on ONE sheet.** Each block is
~39 rows and covers one *project*, with its own account number, its own table, its own totals and
its own signature block. A ~450-row month sheet holds roughly eleven of them.

```
rows   3–13   letterhead: department · title+street · practice · client+month · account no ·
              planner · CONTRACT NUMBER (202-22-746) · contract validity from/to · project
              manager · subject · employee · rate level · employing unit
row      14   header: תאריך │ יום השבוע │ שעת התחלה │ שעת סיום │ סהכ שעות עבודה │ (F) │ מקום העבודה │ תאור העבודה
rows  15–20   data band — SEGMENTS, not whole days
row      21   סה"כ · F=32 (month hours) · G=2815 (carried in) · H=2783 (remaining)
rows  23–28   סה"כ שעות עבודה 32 · מחיר לשעת עבודה 223.97 · סה"כ לחשבון 7167.04 ·
              פלוטים 1%- 7095.37 · מע"מ 18% 1277.17 · סה"כ לחשבון 8372.54
rows  30–37   declaration + signatures
rows  42…     THE NEXT PROJECT BLOCK, same shape
```

**Time segments, not day rows.** `C`=start, `D`=end as Excel time fractions
(`0.291666` = 07:00, `0.583333` = 14:00); `E` = that segment's duration; `F` = the **day** total,
written once on the day's first row and merged down. Mar 2026 rows 15–20: 7h+3h, 8h, 6h+2h, 6h →
`F` column sums to 32.

**A richer billing chain than archetype 1:** hours × rate → **less 1% plots (`פלוטים 1%-`)** → VAT
18% → total. Archetype 1 has no plots line. The billing block is therefore **template-owned**, not
hard-coded — confirming the `TotalsFormulaSet` / `BillingBlock` fields on `DocumentTemplate`.

**Contract awareness is new:** `מספר חוזה` `202-22-746` with validity `44746 → 46206`
(2022-07-01 → 2026-07-01). A row dated past contract expiry is a real-world error worth linting.

**Month gaps are real:** the sheet series skips Aug 2023 → Sep 2024, Jun 2025, and Apr 2026.
Confirms the gap-detection concept (C7) against live data rather than speculation.

### 10.3 What the second archetype changes in the design

| Finding | Design consequence |
|---|---|
| Many report blocks per sheet | `DocumentInstance` must map to a **block within a sheet**, not to a sheet. `DocumentTemplate` gains `BlockHeight` + `RepeatPolicy`. |
| Two archetypes, one family | Validates C2 (template library + fingerprinting). Neither shape is hard-coded; both are derived. |
| Billing chains differ (plots 1%) | The billing formula set is template data, never code. Reinforces C6. |
| Time segments → day totals | The data band has a **two-level** shape: segment rows grouped into a day. `doc.row_extractor` must emit both levels; `doc.formatter` derives the day total. |
| Contract number + validity | New template block + a validator rule: no row dated outside contract validity. |
| `.xls` (BIFF) input | `openpyxl` cannot read `.xls`. The importer needs a legacy path (`xlrd` for read; convert-then-read for round-trip render, since `xlrd` cannot write). |
| Real month gaps | C7 gap detection is a requirement, not a nicety. |
| `מקור` = filename backup | Real versioning replaces it. |

### 10.4 Platform versions (checked, not assumed)

| | Lus | ArmyLuz | Latest |
|---|---|---|---|
| .NET SDK | **9.0.301** (`src/global.json`, `rollForward: latestFeature`) | 9 | 9 |
| Angular | **18.1.0** (`@angular/cli` 18.1.2, TS 5.5.2, zone.js 0.14.3) | **20.0.0** (TS 5.8.2, zone.js 0.15.1) | 22.1.2 |
| `@microsoft/signalr` | absent | ^10.0.0 | — |
| Standalone components | none | 35 files | — |

**.NET needs no upgrade** — Lus is already on 9.0.301.

**Angular: upgrade Lus.UI 18 → 20, not → 22.** The FE we are porting (builder state service, chat
rail, canvas widgets) is written against Angular 20 and signalr v10. Landing on 20 means ported
components compile as written; landing on 22 means migrating the same code twice. 18→20 is two
majors (`ng update` one at a time), not four. 20→22 becomes a separate low-risk follow-up once the
port is green.

---

## 11. The chat entity layer (user request 2026-08-18: "move the entity chats and logic about chats")

The builder session (§1.6) is NOT the chat. ArmyLuz deliberately keeps two layers, and the
BUILDERS_ARCHITECTURE doc says which is which: *"What stays in AIChats/: … chat CRUD
entities/commands/queries, and Suggestions/ — the chat shell is genuinely a chat concern."*

### 11.1 What the chat shell contains (ArmyLuz.Application/AIChats + Contracts/AIChats)

**Entities** (`AiChats/Entities/`):
- `AiChat`: Email/Phone · `AiChatType` · `Closed` · **`AiState` Status** · `GenerateNow` ·
  **`FailedAttempts`** (>= 2 ⇒ message flagged as error and ignored; reset on success) ·
  `LanguageType` · `Messages` · `OrganizationId`.
- `AiMessage`: Sender · IsBot · Text · `Editing`/`TempText` (draft-while-streaming) ·
  **`AiMessageType`** · AiChatId · OrganizationId.

**The message-type system** (`AiMessageType`) is the interesting part — six kinds:
`Default` (visible) · **`Hidden`** (sent to the AI as history, never rendered — e.g. inline rename
commands) · `System` (status/progress) · **`Suggestion`** (interactive card: title, description,
options + hidden AI instruction) · **`SuggestionSelection`** ("You selected: X" bubble carrying the
merged instruction for replay/context) · `ToolResult` (reserved).

**`AiState`** — a lifecycle state machine on the chat: NeedMoreInfo → Started → InProgress →
Ready / GeneratedOnDemand → AwaitingApproval → UserRequestedChanges → …

**Suggestions subsystem** (889 LOC): `IChatSuggestionProvider` + `ChatSuggestionService` (provider
fan-out) + providers (`NewChatSuggestionProvider`, `OrganizationChatSuggestionProvider`) +
`GetChatSuggestions` query + `SubmitSuggestionSelection` (319 LOC) / `DismissSuggestion` commands +
lifecycle DTOs in Contracts (`SuggestionLifecycleState`, `SuggestionActionType`,
`SuggestionMessageKind`…).

**API surface** (`AiChatsController`, `v1/ai_chats`): GetAll · search · delete ·
`{chatId}/suggestions` · `suggestions/select` · `suggestions/dismiss` (+ org-specific routes that
do NOT port).

**FE**: `ChatsManagement/` module (`organization-chat` shell, `chat-suggestion-message`,
`chat-suggestion-selection`) · `aiChatService/{ai-chat.service, chat-suggestion.service}` ·
`ai-chats-table` · `_ai-chat-rail-shared.scss` mixin (shared by both builder rails — copilot mode
tabs, mention composer live on top of it).

### 11.2 The improved chat data-flow logic (user: "we also improved the chat logic bring data flows")

Three docs record the improved understanding pipeline — this is design DNA for `doc.router`, not
code to port verbatim:

**`BUILDER_CHAT_TURN_UNDERSTANDING.md` (post-2026-08-02 design) — the laws:**
1. **Route + Scope.** The router returns the route AND a scope (entities, action, confidence,
   mentions); the orchestrator runs only in-scope agents. Null/low-confidence scope ⇒ full catalog
   (never-regress fallback).
2. **Scope gates, never truncates capability.**
3. **No keyword lists in any language anywhere** — scope is the only gate, so the system is
   language-agnostic.
4. **Deterministic identity before adding** (match → update; ambiguity → question; only true
   strangers are added; a person is never forked).
5. **Ask, don't guess** — missing info becomes a planner question with chips, never a guess.
6. **Honest summary** — DraftPatcher drops value-identical replaces; the summary names real changes
   and no-ops are invisible; "the summary never lies".
7. **No classifier hijack** — the ≤60-char "short text = answer" heuristic was deleted; the router,
   which sees the open question, decides answer-vs-new-instruction.
8. **Derived changes are offered, not imposed** — side-effects surface as a suggestion chip.

**`ORG_CHAT_UNDERSTANDING_LAYERS.md` — layered ownership** (turn scope → domain sets →
requirements → persistence) and the honest documentation of a real failure mode: the good
extraction lane gated on a detail (arrival times) so equivalent phrasings landed in a cruder lane
that collapsed axes (`max()` over shifts). Lesson for the doc builder: **the row-extraction lane
must not gate on incidental detail** — "3 שעות במשרד" and "מ-9 עד 12 במשרד" must land in the same
lane.

**`FRONTEND_AI_ORGANIZATION_CHAT_FLOW.md`** — the FE contract: chat renders
assistant/system/error/progress messages, coordinates SignalR progress, hidden refinement messages
(batched renames) ride `AiMessageType.Hidden`.

### 11.3 Port mapping for Lus

| ArmyLuz | Lus | Notes |
|---|---|---|
| `AiChat`/`AiMessage` entities + repos + CRUD/search | `Lus.Application/Chats/` (`ChatsController`, `v1/chats`) | rename Ai→ chat-generic; drop org-creation-specific commands |
| `AiChatType` enum (org-domain values) | `ChatKind` { DocumentBuilder, … } | domain swap |
| `AiMessageType` 6-kind system | port as-is | the Hidden/Suggestion/SuggestionSelection mechanics are exactly what the doc chat needs |
| `AiState` machine | port with doc-lifecycle states | maps to NeedMoreInfo/InProgress/AwaitingApproval… |
| `FailedAttempts` guard | port as-is | poison-message protection |
| Suggestions subsystem (889 LOC) | port; providers become `NewChatSuggestionProvider` + `DocumentChatSuggestionProvider` | C9's concrete implementation |
| `ChatsManagement` FE + `aiChatService` + rail mixin | port after plan 07 (Angular 20) | the rail mixin is the base of the builder chat |
| Turn-understanding laws (§11.2) | design input to `doc.router` + orchestrator in plan 03 | laws, not code |

---

## 12. The Excel data-import layer (user request 2026-08-18: "excel data import that should also be moved")

Three import lanes exist in ArmyLuz; what ports is the **discipline**, plus the reusable readers.

### 12.1 The lanes

| Lane | Path | Size |
|---|---|---|
| Availability workbook → inquiries | `POST v1/inquiries/apply/import-excel` (dryRun flag) → `ImportAvailabilityWorkbookCommand` (333 LOC C#) → `team_availability_import.py` (692) + `team_availability_extract.py` (2157) + `v2_extraction_schema.py` (144) | ~3.3k |
| Roster/crew import | `create_crew_groups.py` + `roster_import.py` (613; the deterministic person-matcher the chat-turn laws reference) | ~1.2k |
| Generic excel reading | `Ai.PythonEngine/services/excel/{excel_reader (87), schema_detector (110), crew_extractor (191)}` | ~390 |

### 12.2 The import discipline (from AVAILABILITY_EXCEL_INQUIRIES_IMPORT.md — "non-negotiable gates")

1. **Dry-run first** — the endpoint carries a `dryRun` flag; preview before write.
2. **Read-before-write preflight** — read the server's actual schema/state and echo it
   byte-for-byte; never hardcode assumptions (never בוקר=06:00).
3. **Unclear → skip and report, never guess** — names resolve deterministically; ambiguity is
   reported, not resolved by force.
4. **Canary → confirm → full run → verify by re-read + full diff; 0 diffs = done.**
5. **Idempotent upsert keys** — re-runs are safe; corrections are resubmission.
6. **Never use the wipe-lane** — the bulk overwrite endpoint that destroys prior state is
   documented as forbidden for imports.
7. **Deterministic gates as code** (G1–G8), with their own pytest suite.

### 12.3 Multilingual parsing rules (EXCEL_CREW_IMPORT_RULES.md)

"The importer is multilingual and must stay smart. It should not depend on hardcoded Hebrew role
names." — split on `,;|`, split slashes only between full roles, keep suffix/gender notation
intact, canonicalize variants. Same no-keyword-lists law as the chat pipeline.

### 12.4 Port mapping for Lus

The Document Builder's import is C14 (`doc.template_reader` backfill). What §12 adds to C14:

- The **discipline** (§12.2) becomes the C14 contract: import runs dry-run first, previews as
  draft patches on the canvas, commits only after confirm, verifies by re-read diff.
- `excel_reader.py` + `schema_detector.py` (390 LOC, generic) port as the base of
  `doc.template_reader`.
- `roster_import.match_person`'s deterministic-match-else-ask pattern becomes the pattern for
  matching historical sheet rows to existing `DocumentInstance`s/subjects.
- `team_availability_extract.py`'s two-phase shape (extract → schema-validated intermediate →
  import) is the shape of backfill: `.xls/.xlsx` → extraction JSON (schema-validated) → entity
  writes through commands.

---

## 13. Defect classes the port must NOT inherit

Source: a peer session working in ArmyLuz flagged `ArmyLuz/.brain/office-brief-generic-robustness.md`
(2026-08-18) — a live audit where a *simple* Hebrew brief scored only ~30–45% honored while harder
briefs passed. Every root cause was an already-catalogued defect class, not a capability gap. These
are exactly the failure modes a port inherits for free if nobody names them.

| # | Defect class | What happened in ArmyLuz | The Lus/document analogue to guard against |
|---|---|---|---|
| 1 | **Scaffold stowaway** | Archetype scaffold entities survived after the brief contradicted them (5 invented positions kept; a 07–20 window survived a stated 08–18) | Template scaffold rows/values surviving after the user's dictation contradicts them |
| 2 | **Lossy re-route** | Items parked in a `pendingDemandWindows` queue with empty start/end that **never became rules** — silently dropped | A patch queued by one agent that no later stage applies; a parsed value that reaches no cell |
| 3 | **Entity minting** | The arrival ladder **invented a shift** ("בוקר 08–10") nobody stated | Inventing a day, segment, or row the user never dictated |
| 4 | **Misclassification loses data** | A department became an `Assignment(post)` and its counts were lost | A dictated segment classified as the wrong block, dropping its hours |
| 5 | **Generic-pool capture** | Count 4 bound onto a generic catch-all entity ("עובד כללי") with mangled merged windows | Hours binding to the wrong day/subject because a generic bucket matched first |
| 6 | **False all-clear** | The turn ended "נראה מוכן!" while whole sentences were missed; deterministic fallbacks emitted **0 patches and 0 notes** | A turn reporting success while silently dropping part of what the user said |

### 13.1 The fix pattern worth porting: a per-sentence verdict ledger

ArmyLuz's remedy is the part to copy. Rather than trusting a summary, **every input sentence gets an
explicit verdict** — `understood` / `partial` / `missed` / `can't-represent` — and that ledger is
simultaneously:

- **the user's control surface** (confirm/correct per sentence, routed back through the normal
  refine path), and
- **the test gate** (a golden-brief honored-rate wall, ≥80% aiming at 90%, enforced by tests).

Two supporting laws from the same doc:

- **A veto re-routes, never discards** (their "law 12"): when a judge rejects an extraction, the
  content goes somewhere else — it is never silently dropped.
- **Zero patches must never mean zero notes**: a deterministic fallback that produces no output must
  emit a note naming the unhandled input.

This strengthens spec §4.5.1 law 5 ("the summary never lies") from a prohibition into a mechanism:
it is not enough to avoid lying in the summary; the system must *account for every sentence*.

Tracked as concept **C19** in `docs/DOCUMENT_BUILDER_SMART_CONCEPTS.md` and as an explicit
non-goal-inheritance list for plan 03.
