# Document Builder — Smart Concepts

> The end state. What "smart" means concretely, concept by concept, each with the mechanism that
> delivers it and the failure it prevents.
>
> Spec: [`superpowers/specs/2026-08-18-ai-builders-port-design.md`](./superpowers/specs/2026-08-18-ai-builders-port-design.md) ·
> Research: [`../.brain/2026-08-18-armyluz-to-lus-port-research.md`](../.brain/2026-08-18-armyluz-to-lus-port-research.md)

## The one-sentence goal

**You show it an example, you talk to it, and it produces the document — and you can still grab any
cell and fix it by hand.**

Three surfaces, one state:

```
  ┌─────────────────┬──────────────────────────────────┐
  │   CHAT RAIL     │        EXCEL CANVAS              │
  │                 │                                  │
  │ you dictate     │  live, editable sheet preview    │
  │ it asks         │  click a cell → edit → patch     │
  │ chips suggest   │  derived cells shown, not typed  │
  │ ticker narrates │  changed cells animate           │
  └─────────────────┴──────────────────────────────────┘
                    ▼
              ONE DRAFT, ONE PATCH STREAM
                    ▼
            .xlsx  or  .pdf  (your choice)
```

**The law that makes it coherent:** chat edits and hand edits are *the same operation*. A human
clicking a cell emits the same patch op an agent emits. There is no second write path, so undo,
redo, validation, and the version guard work identically for both.

---

## C1 — Template learning (the exemplar is the spec)

**What:** Upload `my-report.xlsx`. The system reads it and derives a `DocumentTemplate`: the five
blocks, the data band, the merge policy, the formula set, RTL, column widths, print setup.

**Mechanism:** `doc.template_reader` (Importer kind, deterministic, no LLM) using `openpyxl`.
Block detection is structural — a label column with no data band beneath it is a totals/billing
block; the row where a date column starts a contiguous run is the data band start.

**Smart part:** the template captures *intent*, not just geometry. It records that column C is
"hours, numeric, summed into the totals row" — so when the band grows, the sum follows.

**Prevents:** rebuilding 31–45 merged ranges by hand and getting them subtly wrong.

**Escalation:** where structure is ambiguous, `doc.schema_planner` (LLM) names the columns and the
user confirms via chips. Ambiguity is resolved once, then stored on the template.

---

### C1a — Two archetypes prove the template is data, not code

Two real document families are already in evidence, and they disagree on almost everything that a
naive implementation would have hard-coded:

| | Archetype 1 — hours account (`.xlsx`) | Archetype 2 — estimate register (`.xls`) |
|---|---|---|
| Client | רשות שדות התעופה | עירית תל אביב, אגף דרכים ומאור |
| Cardinality | one report per sheet | **~11 report blocks stacked per sheet** |
| Data band | one row per work item | **two-level**: time segments grouped into days |
| Billing chain | hours × rate → VAT 18% | hours × rate → **less 1% plots** → VAT 18% |
| Contract | none | number + validity window |
| Format | `.xlsx` (openpyxl) | `.xls` BIFF (**xlrd to read; cannot write**) |

Everything in that table lives on `DocumentTemplate`. Nothing in it is a branch in the code. The
two-level `DocumentDay` → `DocumentRow` model serves both because archetype 1 is the degenerate
case: exactly one segment per day, no times.

**The `.xls` asymmetry is worth naming.** `xlrd` reads BIFF but cannot write it, so a `.xls` source
cannot be round-tripped the way C15 round-trips `.xlsx`. A `.xls` series is converted to `.xlsx`
once at import and rendered from there.

---

## C2 — Template library and fingerprinting

**What:** Templates are reusable. Upload a second workbook of the same shape and the system
recognises it rather than re-deriving from scratch.

**Mechanism:** a structural fingerprint (header labels + column count + merge topology, normalised
for whitespace) indexed per organization. A match offers "this looks like *Monthly hours account* —
use that template?".

**Smart part:** whitespace-normalised matching. The exemplar's own sheet names differ only by
stray spaces (`'מרץ 2025 '`, `' אפריל 2026 '`); a fingerprint that respected them would treat
identical sheets as different templates.

**Prevents:** every new document being a cold start.

---

## C3 — The editable canvas

**What:** The canvas is not a read-only preview. Click a cell, type, and it commits — while an agent
may be writing other cells in the same turn.

**Mechanism:** a cell edit emits `{"Op":"UpdateRow","Path":"rows[3].hours","Value":4}` through the
same `canvasCommand` turn an agent uses. `DraftPatcher` applies it under the optimistic version
guard. Concurrent agent output and human output are serialised by version, and a stale version is
rejected with the current draft so the client re-bases.

**Smart part:** derived cells are **not** editable as values. Clicking the total shows *why* it is
what it is (C10) and offers the inputs that produce it. You cannot type over a formula and create
the exemplar's defect 4.

**Prevents:** two write paths drifting apart; hand-typed values shadowing formulas.

---

## C4 — Conversational fill (dictation → rows)

**What:** *"5 במרץ, 3 שעות במשרד, התייעצות מחדש עם מתכנן התנועה על הסדרי תנועה זמניים במחלף נתב״ג
מערב"* becomes a row.

**Mechanism:** `doc.row_extractor` (Content, LLM, content tier) returns row patches.

**Smart parts:**
- **Day-of-week is derived, never asked.** `2026-03-05` → `ה`.
- **Multi-row turns.** One sentence describing three activities on one date yields three rows that
  share a merged date cell.
- **Location defaults.** `מיקום` is `משרד` in the overwhelming majority of historical rows; it is
  pre-filled and only asked when the text implies otherwise (`רש"ת`).
- **Relative dates resolve against the sheet's period.** "yesterday" inside the March sheet is
  March-relative, not today-relative.

**Prevents:** the form-by-form data entry that `create-project` / `data-loader` demand today.

---

## C5 — History as context (the 19-month memory)

**What:** The supplied workbook carries 19 months of prior rows. That is not dead data — it is the
best available model of how this user works.

**Mechanism:** a grounding loader passes a compacted digest of prior sheets to the extractor and the
suggestion providers: recurring subjects, typical hours per subject kind, location distribution,
active project threads.

**Smart parts:**
- **Subject continuity.** "המשך הדיון על מחלף נתב״ג מערב" links to the running thread rather than
  reading as a new topic.
- **Typical-hours prior.** A subject historically logged at 2h flags a 12h entry for confirmation.
- **Thread awareness.** "ועדת תנועה 02/26" is recognised as the same committee cycle across months.

**Prevents:** an assistant that has to be told everything every month.

---

## C6 — The derivation engine (numbers are computed, never generated)

**What:** Sum, carry-in, remaining, rate, VAT, total are calculated by code.

**Mechanism:** `doc.formatter` and `doc.carry_forward` are **LLM-free by design**. Rates come from
the `RateCard` entity, not from a sheet and not from a model.

```
C = SUM(data band)
D = previous instance's remaining        (resolved by entity id, not by cell address)
E = D − C
subtotal = hours × rate ; vat = subtotal × vatPercent ; total = subtotal + vat
```

**Smart part:** the carry-in is resolved through `CarryInFromInstanceId` — an entity relationship —
and only rendered *down* to a cell reference at write time, pointing at wherever the previous
sheet's remaining cell actually landed.

**Prevents:** exemplar defects 1 and 4, and any possibility of an LLM inventing a billing figure.

---

## C7 — The series brain (a workbook is a chain, not a pile)

**What:** "Start next month" is one action.

**Mechanism:** `doc.carry_forward` clones the template, clears the data band, names the sheet from
the period, derives the account number (`DDMMYY01`), sets the declaration date to the period's last
day, and links `CarryInFromInstanceId` to the prior instance.

**Smart parts:**
- **Period detection.** Uploading a workbook infers which months exist and which is next.
- **Gap detection.** A missing month in the chain is surfaced, not silently skipped.
- **Chain integrity.** Because links are entity ids, renaming a sheet cannot break the chain.

**Prevents:** exemplar defects 1, 2 and 3 — the hand-typed cross-sheet reference, the whitespace-
sensitive sheet name, and the copy-paste layout drift.

---

## C8 — The validator (lint that blocks a bad document)

**What:** A hard pass before commit, plus soft warnings live on the canvas.

**Mechanism:** `doc.validator` (Validator kind, deterministic) with auto-fixes for the unambiguous
cases.

**Hard blocks:**
- Empty rate, subtotal, or VAT when the billing block exists — **this is the live money bug**.
- A total that renders `0.00` with a non-empty data band.
- A broken or missing carry-in link.
- A required column empty in any row.

- **A row dated outside the contract's validity window** (archetype 2 carries
  `מספר חוזה 202-22-746`, valid 2022-07-01 → 2026-07-01). Billing work to an expired contract is a
  real-world error the spreadsheet cannot currently catch.

**Soft warnings:** duplicate date+subject; hours outside the historical range for that subject;
a month whose total departs sharply from the trailing average; a date outside the sheet's period;
overlapping time segments within one day (archetype 2).

**Prevents:** exemplar defect 5 — the two ₪0.00 invoices that actually shipped.

---

## C9 — Suggestions and copilot mode

**What:** Two modes on the same rail — **build** (fills the document) and **advisor** (answers
questions about it, changes nothing).

**Mechanism:** chip suggestions via provider fan-out; `doc.advisor` (Advisor kind) is read-only by
construction — it produces no patches.

**Smart parts:** chips are grounded in the actual draft — *"add the 2h you usually log for ועדת
תנועה"*, *"March is 8h below your average, missing entries?"*, *"close the month and start April"*.

**Prevents:** a blank chat box the user has to think their way out of.

---

## C10 — Explainability

**What:** Every derived cell can answer *"why is this 728?"*.

**Mechanism:** the derivation engine records provenance per computed cell — inputs, operation,
source entity — carried on the draft and surfaced on click.

> `E25 = 728` ← `D25 (760, carried in from פברואר 2026) − C25 (32, sum of 14 rows)`

**Prevents:** a spreadsheet nobody trusts because nobody can retrace it.

---

## C11 — The intent router (one turn brain)

**What:** The user types one thing and the system works out what kind of thing it was.

**Mechanism:** `doc.router` classifies each turn — dictation · correction · question · command
(*"start April"*, *"export PDF"*) · import — and dispatches the matching wave. Unclassifiable turns
go to `doc.question_planner` for one clarifying question rather than a guess.

**Smart part:** corrections are recognised as corrections. *"actually that was 4 hours"* patches the
last-touched row instead of adding a new one.

**Prevents:** mode switches the user has to perform manually.

---

## C12 — Cost-aware model routing

**What:** The cheapest model that can do the job, per agent.

**Mechanism:** ported `pyutil/model_router.py`. Agents declare a capability tier centrally
(`lite · chat · content · deep`); a deterministic regex/counting score gates escalation.

**Laws:** 90–95% of calls never reach `deep` · length alone can never escalate · budget-aware
(`warning` prefers one tier down, `critical` caps at `chat`) · **parity when off** — with no tier
env set, every route returns the legacy single model · one-tier ladder retry on an empty answer.

Metering: `COST:{…}` on stderr per call, attributed by agent name.

**Prevents:** a per-document cost nobody predicted.

---

## C13 — Narrated progress

**What:** A multi-agent wave takes seconds. The user watches it happen instead of watching a spinner.

**Mechanism:** SignalR `AgentStatus` drives a ticker (*"מחלץ שורות…"*), `DraftPatched` animates the
cells each agent touched.

**Laws:** nothing blocks the whole screen · narration is enhancement-only — with the hub down the
turn still completes over HTTP and the user loses the ticker, not the work · percentages are honest
only where a real denominator exists.

**Prevents:** the blocking overlay Lus runs today, which makes a 20-second wave unusable.

---

## C14 — Import and backfill

**What:** Existing history comes in wholesale, not by re-typing.

**Mechanism:** the same `doc.template_reader` pass that derives the template also ingests the
historical sheets as `DocumentInstance` + `DocumentRow` entities, reconstructing the chain from the
cross-sheet references it finds.

**Smart parts:**
- It repairs while importing — resolves whitespace-sloppy sheet names to real instances and
  rebuilds the chain by entity id.
- **The import discipline** (ported from ArmyLuz's productized Excel-import lane, spec §4.6):
  dry-run first — the import previews as draft patches on the canvas before anything commits;
  unclear → skip and report, never guess; canary → confirm → full run → verify by re-read + full
  diff (0 diffs = done); idempotent upsert keys so re-import is safe.
- **Two-phase shape**: extract to a schema-validated intermediate, then write through canonical
  commands — never parse-and-write in one step.
- **Multilingual cell parsing with no hardcoded vocabulary** (split `,;|`, slash only between full
  values, canonicalize variants) — the same no-keyword-lists law as the chat.

**Prevents:** starting from an empty system when 19 months of truth already exist — and a bad
import silently corrupting what does exist.

---

## C15 — Format at the end, not the beginning

**What:** `.xlsx` or `.pdf` is chosen at export, not at the start.

**Mechanism:** one draft, two renderers. `xlsx_renderer` reopens the exemplar and writes into it —
styles, merges, widths, print setup **inherited, never rebuilt**. `pdf_renderer` renders the same
draft page-set.

**Prevents:** two divergent document models, and the fidelity loss of rebuilding a workbook from
scratch.

---

## C16 — Guardrails (what the system refuses to do)

1. **No invented numbers.** Money and balances come from the derivation engine.
2. **No commit past a hard validation failure.**
3. **No direct writes.** Patches are the only mutation path — for agents and humans alike.
4. **No silent overwrite.** Stale-version turns are rejected with the current draft.
5. **No agent touches the database.** Agents are pure functions.
6. **No user-facing internals.** Tracebacks to stderr; users see bilingual safe messages.

---

## C18 — The chat shell (persistent conversations, six message kinds)

**What:** The chat is not ephemeral. Conversations persist, resume, and carry structure — ported
from ArmyLuz's `AIChats` layer, the part its architecture doc calls "genuinely a chat concern".

**Mechanism:** `Chat` + `ChatMessage` entities (`Lus.Application/Chats/`, `v1/chats`), separate
from the builder session — the chat references a build session; neither owns the other.

**Smart parts:**
- **Six message kinds**, each with distinct rendering and AI-visibility:
  `Default` · **`Hidden`** (in the AI's history, never rendered — how the FE sends batched
  refinement commands) · `System` · **`Suggestion`** (interactive card with options + a hidden AI
  instruction) · **`SuggestionSelection`** (compact "you selected X" bubble carrying the merged
  instruction for replay) · `ToolResult`.
- **A chat state machine** (`NeedMoreInfo → InProgress → Ready → AwaitingApproval →
  UserRequestedChanges…`) so the UI always knows what the conversation is waiting for.
- **`FailedAttempts` poison-message guard** — two consecutive processing failures flag the message
  and stop retrying it, reset on success. A malformed message cannot wedge a chat forever.
- **The turn-understanding laws** (spec §4.5.1): route+scope gating, no keyword lists in any
  language, deterministic identity before adding, ask-don't-guess, the-summary-never-lies, no
  classifier hijack, derived changes offered not imposed, and lanes never gate on incidental
  detail.

**Prevents:** a stateless chat that forgets everything on refresh, renders its own plumbing, or
loops forever on a poisoned message.

---

## C19 — The per-sentence verdict ledger (nothing is silently dropped)

**What:** Every sentence you say gets an explicit verdict — **understood · partial · missed ·
can't-represent** — visible to you, per turn.

**Mechanism:** the orchestrator emits a ledger row per input clause alongside the patches. The
ledger is *both* the user's control surface (confirm or correct each row, routed back through the
normal refine path) *and* the test gate (a golden-dictation honored-rate wall enforced in CI).

**Why it exists:** a peer audit of ArmyLuz (`.brain/office-brief-generic-robustness.md`, 2026-08-18)
found a *simple* brief scoring ~30–45% honored while harder ones passed — and the turn still ended
with "נראה מוכן!" ("looks ready!"). Every root cause was a known defect class. The lesson: a
summary that avoids lying is not enough; the system must **account for every sentence**.

**Two supporting laws:**
- **A veto re-routes, never discards.** When a validator or judge rejects an extraction, the content
  goes somewhere else — never silently nowhere.
- **Zero patches must never mean zero notes.** A lane that produces no output emits a note naming
  the input it could not handle.

**The six defect classes this guards against** (all observed in ArmyLuz, all portable for free if
unnamed):

| Defect | The document-builder form |
|---|---|
| Scaffold stowaway | template scaffold values surviving after your dictation contradicts them |
| Lossy re-route | a patch queued by one agent that no later stage ever applies |
| Entity minting | inventing a day, segment, or row you never dictated |
| Misclassification loses data | a segment routed to the wrong block, dropping its hours |
| Generic-pool capture | hours binding to the wrong subject because a generic bucket matched first |
| False all-clear | reporting success while silently dropping part of what you said |

**Prevents:** the most expensive failure mode in this whole system — confidently producing a
document that is quietly missing a third of what you told it.

---

## C17 — Real versioning (retiring `מקור`)

**What:** History and rollback, instead of a duplicate file with `מקור` in its name.

**Mechanism:** the draft's monotonic `Version` plus the inverse patch batch recorded for every
apply; `DocumentInstance.Status` (`Draft → Committed → Rendered`) for the lifecycle. Every render is
attributable to an instance version.

**The evidence:** both supplied workbooks ship with a `מקור` twin, and a full cell-by-cell diff of
the `.xlsx` pair returns **0 differing cells** across all 19 sheets. It is not a blank master — it
is a backup taken before editing. There is no history, no diff, and nothing but the filename to say
which copy is authoritative.

**Prevents:** "which file is the real one?" — and the silent divergence that follows once someone
edits the wrong twin.

---

## Capability map

| Concept | Agents | Kind | LLM | Phase |
|---|---|---|---|---|
| C1 Template learning | `doc.template_reader`, `doc.schema_planner` | Importer, Content | partial | P4 |
| C2 Template library | — (service + fingerprint index) | — | no | P4 |
| C3 Editable canvas | — (patch stream) | — | no | P5 |
| C4 Conversational fill | `doc.row_extractor` | Content | yes | P4 |
| C5 History as context | — (grounding loader) | — | no | P4 |
| C6 Derivation engine | `doc.formatter` | Content | **no** | P4 |
| C7 Series brain | `doc.carry_forward` | Content | **no** | P4 |
| C8 Validator | `doc.validator` | Validator | **no** | P4 |
| C9 Suggestions / copilot | `doc.advisor` + providers | Advisor | yes | P5 |
| C10 Explainability | — (provenance on the draft) | — | no | P4 |
| C11 Intent router | `doc.router`, `doc.question_planner` | Content, Planner | partial | P3 |
| C12 Model routing | — (`pyutil/model_router`) | — | no | P3 |
| C13 Narrated progress | — (SignalR + reporter) | — | no | P3 |
| C14 Import / backfill | `doc.template_reader` | Importer | no | P4 |
| C15 Two renderers | — (`xlsx_renderer`, `pdf_renderer`) | — | no | P6, P7 |
| C16 Guardrails | all | — | — | every |
| C17 Real versioning | — (draft version + inverse patches) | — | no | P3 |
| C18 Chat shell | — (Chat/ChatMessage entities + suggestions subsystem) | — | no | P3 |
| C19 Verdict ledger | — (orchestrator ledger + golden-dictation wall) | — | no | P3 |

Ten of the nineteen are deliberately **LLM-free**. The intelligence that touches money is arithmetic.

---

## What is NOT in scope

- Editing the *template* by conversation. Templates change by uploading a new exemplar.
- Multi-user simultaneous editing of one draft. The version guard rejects the loser; it does not merge.
- Arbitrary spreadsheet formulas authored by the user. The formula set is the template's, not free-form.
- Inferring a template from a PDF. PDF is an output format only.
