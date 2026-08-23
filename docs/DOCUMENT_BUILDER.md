# Document Builder

> Status: **specified 2026-08-18, not yet implemented.** Spec: [`docs/superpowers/specs/2026-08-18-ai-builders-port-design.md`](./superpowers/specs/2026-08-18-ai-builders-port-design.md). Research: [`.brain/2026-08-18-armyluz-to-lus-port-research.md`](../.brain/2026-08-18-armyluz-to-lus-port-research.md).

## What it is

A monthly work-hours account (`דוח ביצוע שעות עבודה`) from a practice to a client. The user uploads an exemplar workbook to show how the output should look, then *talks* to the system. The system fills that document and emits `.xlsx` or `.pdf`.

This replaces manual data entry (`create-project` / `data-loader` / `project-validator`) and the hand-rolled browser Excel writer as the path for **new** documents. `excel-export.component.ts` stays as the legacy `ProjectTemplate` export flow, frozen.

Money values are computed deterministically. The LLM never invents billing figures.

## The document is a series, not one sheet

The builder unit is the **workbook**. The exemplar chains a hours-balance across months by cross-sheet reference.

**Workbook series is per client-project and open-ended.** The year in the filename (`…ר.ש.ת 2026-אא.xlsx`) is a label, not a boundary. Evidence: that file contains sheets from December 2024 onward, and the balance chain crosses 2025→2026 unbroken. `DocumentSeries` has no year column.

If this is later wrong, the correction is a `Year` column on `DocumentSeries` plus a chain-break rule at the January sheet — contained to P3, not a redesign.

## Five blocks of a month sheet

| Block | Content |
|---|---|
| Title | client name; `דוח ביצוע שעות עבודה <month year>` |
| Letterhead | practice name, address, phone, VAT id; `לקוח`; date; `המתכנן`; `מספר ח-ן` |
| Table header | `תאריך │ יום השבוע │ סהכ שעות עבודה │ מיקום │ נושא העבודה` |
| **Data band** | one row per work item; **A and B merged vertically** across rows sharing a date |
| Totals + billing + declaration | `סה"כ` · `=SUM(C…)` · carry-in · remaining; rate · subtotal · VAT 18% · total; signatures |

All sheets `rightToLeft=True`. Column widths inherited from the exemplar. 31–45 merged ranges per sheet.

`DayOfWeek` is **derived from `Date`**, never asked and never typed.

## Balance mechanic

```
C = SUM(data band)                     hours consumed this month
D = ='<previous month sheet>'!E<row>   balance carried in
E = D - C                              balance remaining
```

The remaining-cell **row number moves** every month because the data band length varies. The renderer must repoint the carry-in reference at the previous instance's *actual* remaining-cell, and must tolerate leading/trailing/double-space sheet names (byte-exact Excel formula matching).

## Entities

```
DocumentSeries          the workbook. Per client-project, open-ended.
  Id · OrganizationId · Name · ClientName · TemplateId · ExemplarFileId · CreatedAt

DocumentTemplate        derived from the exemplar; the draft skeleton.
  Id · SeriesId · Rtl · ColumnWidths · LetterheadFields · TableHeader
  DataBand{StartRow, MergePolicy} · TotalsFormulaSet · BillingBlock · DeclarationBlock

DocumentInstance        one month/sheet in the series.
  Id · SeriesId · PeriodStart · PeriodEnd · SheetName · AccountNumber
  CarryInFromInstanceId · Status{Draft,Committed,Rendered}

DocumentRow             a row in the data band.
  Id · InstanceId · Ordinal · Date · DayOfWeek · Hours · Location · Subject

DocumentBuildSession    live builder session (Redis + this DB rescue row).
  Id · UserId · InstanceId · SchemaVersion · Version · DraftJson · UpdatedAt

RateCard                rates out of the stale sheet and into storage.
  Id · SeriesId · EffectiveFrom · HourlyRate · VatPercent
```

## Draft & patch ops

The draft is the JSON projection of one `DocumentInstance` + its rows + resolved template. Agents never return a whole draft — they return **patch ops**:

```
{ "Op": "AddRow"|"UpdateRow"|"RemoveRow"|"SetField"|"SetTotals",
  "Path": "rows[3].hours" | "letterhead.accountNumber" | …,
  "Value": … }
```

`DraftPatcher` applies a batch atomically under an optimistic `Version` guard and records the inverse batch so undo/redo is free.

## Agents

| Agent | Kind | Wave | LLM | Does |
|---|---|---|---|---|
| `doc.template_reader` | Importer | — | no | exemplar `.xlsx` → `DocumentTemplate` |
| `doc.carry_forward` | Importer | — | no | clone last month, clear data band, repoint the chain, seed letterhead + account number |
| `doc.schema_planner` | Content | 1 | yes | name each column's semantic meaning + type |
| `doc.row_extractor` | Content | 2 | yes | work-log prose → row patches |
| `doc.formatter` | Content | 3 | no | sum, carry-in, remaining, rate, VAT, total; merge-by-date policy |
| `doc.validator` | Validator | — | no | type/required/duplicate lint + deterministic auto-fix; **empty rate blocks commit** |
| `doc.reviewer` | Content | 4 | yes | final coherence pass before commit |
| `doc.question_planner` | Planner | — | no | pick the single next question |
| `doc.advisor` | Advisor | — | yes | grounded free-text Q&A about the document |

`doc.formatter` and `doc.carry_forward` are **LLM-free**. That is the direct answer to the live money bug (empty rate → `0.00` on issued PDFs).

Model tiers: `row_extractor` / `schema_planner` / `reviewer` = `content` (deep-eligible); `advisor` = `chat`. Deterministic agents get no row. 90–95% of calls must never reach deep.

## One turn

1. User types into the chat rail (or uploads a file).
2. `POST v1/documents/builder/turn` with `{sessionId, version, text|fileId}`.
3. Orchestrator classifies (dictation / question / correction / command).
4. Runs the wave via `PythonScriptsAdapter.RunAgentAsync` → `runner.py --agent doc.<name>`.
5. Patch ops applied under the version guard.
6. `DraftPatched` over SignalR → canvas animates changed cells. `AgentStatus` drives the ticker.
7. `doc.question_planner` emits chips. Session to Redis (7-day TTL) + DB rescue.
8. HTTP response returns the new version + ops (the turn works with SignalR down).

## Commit and render

`POST v1/documents/builder/commit` → `doc.validator` hard pass → `DocumentCommitService` maps the draft into `CreateDocumentInstanceCommand` / `UpsertDocumentRowsCommand` → renderer.

**xlsx (`render/xlsx_renderer.py`):** reopen the **exemplar workbook** with `openpyxl`, clone the template sheet, name it for the period, write the data band, apply merge-by-date, write totals/billing formulas, repoint the carry-in reference. Styles, merges, widths, print setup are **inherited, never rebuilt**.

**pdf (`render/pdf_renderer.py`):** same draft, page-set renderer. The exemplar PDFs are the print of the sheet.

## Defects the builder kills

1. Hand-typed cross-sheet refs whose row moves monthly → series owns the chain.
2. Inconsistent sheet-name whitespace that formulas must match byte-exact → renderer resolves the actual sheet.
3. Layout drift (billing block column C in 2025, column D in 2026) → round-trip the exemplar, don't rebuild.
4. Hand-typed duplicates of computed values → derived values are computed, never typed.
5. Empty rate → zero-total invoices shipped → validator blocks commit.
6. `תעריפים` doing double duty as master template and rate store → `RateCard` entity.

## Tests that pin this

- Golden-file render against the supplied workbook (styles, merges, widths, RTL, formula strings).
- Three-month balance chain: `E(n) = D(n) − C(n)` and `D(n+1) = E(n)` at the actual remaining-cell row.
- Sheet-name tolerance (leading/trailing/double space).
- Empty rate fails `doc.validator` and blocks commit.
- Hebrew round-trip C# → stdin → Python → stdout → draft → render, with and without BOM.
- Derived day-of-week never asked, never typed.
