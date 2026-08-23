# P4 — Doc agents + template_reader on the real exemplar

> Parent + spec §4.4, §10.2. Exemplar: `/Users/onecity/Downloads/attachments/חשבון שעות לפרויקטים ר.ש.ת 2026-אא.xlsx`

**Goal:** Real `doc.*` agents. The supplied workbook parses into a `DocumentTemplate` with all five blocks identified.

**Exit criterion:** That parse succeeds in a pytest against a committed copy of the exemplar (or a stripped fixture if the original cannot be committed — see Task 1).

**Depends on:** P3 draft/session. OpenAI key required only for LLM agents; `template_reader`, `carry_forward`, `formatter`, `validator`, `question_planner` are keyless.

---

### Task 1: Golden fixture

Copy the exemplar into `PythonScripts/tests/golden/rst-hours-series.xlsx`. If the file is too large or confidential for git, commit a **structurally identical** workbook: same sheet names (including the messy whitespace), same merged ranges, same formulas, synthetic hours. Prefer the real file if the owner agrees.

Also copy one monthly PDF into `PythonScripts/tests/golden/` for P7.

---

### Task 2: `doc.template_reader` (Importer, no LLM)

**Files:**
- `PythonScripts/agents/doc/template_reader.py`
- `PythonScripts/agents/schemas/template_reader.result.schema.json`
- `PythonScripts/tests/agents/doc/test_template_reader.py`

Reads the workbook with openpyxl (`data_only=False`). For a representative month sheet (prefer `מרץ 2026` if present, else the last non-`תעריפים` sheet):

Identify:
- Rtl = `sheet.sheet_view.rightToLeft`
- ColumnWidths for A–F
- Title rows, letterhead fields (scan known Hebrew labels: `לקוח`, `המתכנן`, `מספר ח-ן`)
- Table header row (the five column titles)
- DataBand.StartRow = header + 1
- MergePolicy = "group-date-columns-AB"
- TotalsFormulaSet (find `סה"כ` row)
- BillingBlock (rate / VAT / total labels)
- DeclarationBlock start row

Result patches: `SetField` ops onto `template.*`.

Register as `doc.template_reader` → canonical `template_reader`.

Test: five blocks present; `Rtl is True`; data band start ≥ 11; at least 30 merged ranges recorded.

---

### Task 3: Deterministic agents (no LLM)

| Agent | File | Behavior |
|---|---|---|
| `doc.formatter` | `formatter.py` | Recompute C=SUM, D=carry-in, E=D−C, billing from RateCard. Merge A/B by date. Emit SetTotals + merge ops. |
| `doc.validator` | `validator.py` | Required fields; duplicate dates+subject; **empty/zero rate → failure code `empty_rate` that blocks commit**. Auto-fix day-of-week from Date. |
| `doc.carry_forward` | `carry_forward.py` | Clone last instance, clear data band, bump account number, set CarryInFromInstanceId, sheet name for period. |
| `doc.question_planner` | `question_planner.py` | If no rows → ask for first work-log line. If empty rate → ask for rate. Else null. |

Wire into `DocumentBuilderAgentCatalog` with correct kinds/waves.

Tests:
- formatter: 2 rows of 4h + carry-in 100 → remaining 92
- validator: empty rate → not Ok / `empty_rate`
- day-of-week derived, never taken from input
- carry_forward: data band empty, chain pointer set

---

### Task 4: LLM agents (content tier)

| Agent | Tier | Behavior |
|---|---|---|
| `doc.schema_planner` | content | Map columns to Date/Day/Hours/Location/Subject |
| `doc.row_extractor` | content | Work-log Hebrew prose → AddRow patches. Date parse (5 במרץ / 5.3.2026). Hours numeric. Subject rest of line. |
| `doc.reviewer` | content | Coherence pass; no money invention (prompt law). |
| `doc.advisor` | chat | Q&A grounded in draft JSON only. |

Use `pyutil.model_router`. Add openai/langchain to `requirements.txt` now.

Tests without network: mock the LLM to return a fixture patch list. One live test marked `@pytest.mark.integration` skipped unless `OPENAI_API_KEY` is set.

Prompt law in each LLM agent: "Never invent numeric hours, rates, VAT, or totals. If missing, omit the field."

---

### Task 5: Catalog + runner registry

Update `_registry()` and `AGENT_ALIASES` for every `doc.*` name in the spec table. `test_runner_aliases.py` fails if an alias has no module. `test_model_router.py` fails if an LLM agent has no tier row.

Wire catalog waves: schema_planner=1, row_extractor=2, formatter=3, reviewer=4.

**P4 done when:** `pytest PythonScripts/tests/agents/doc/test_template_reader.py` identifies all five blocks on the golden workbook; validator blocks empty rate; aliases test green.
