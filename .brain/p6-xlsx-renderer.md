# P6 — Commit + openpyxl round-trip xlsx renderer

> Parent + spec §6.1, §10.2 golden-file tests.

**Goal:** Commit maps the draft through canonical MediatR commands, then `render/xlsx_renderer.py` writes a workbook that looks like the exemplar.

**Exit criterion:** Golden-file test green against the supplied workbook.

**Depends on:** P4 template + formatter + validator.

---

### Task 1: Commit commands (never bypass)

**Files:**
- `CreateDocumentInstanceCommand` + handler
- `UpsertDocumentRowsCommand` + handler
- `DocumentCommitService` — draft → those commands only
- `POST v1/documents/builder/commit`
- Hard-fail if `doc.validator` reports `empty_rate`

Tests: commit with empty rate → 400; commit with rows + rate → Instance Status=Committed, rows persisted, org-scoped.

---

### Task 2: `xlsx_renderer.py`

**Law:** reopen the **exemplar** with openpyxl. Clone the template sheet. Do not create a Workbook() from scratch. Do not rebuild 31–45 merged ranges by hand — they are inherited from the clone, then the data-band merges are re-applied for the actual row count.

Steps:
1. Load exemplar
2. Clone the month-template sheet (the one `template_reader` recorded)
3. Rename to the period (`מרץ 2026`) — **trim inconsistent whitespace** on the new name; keep a stored `ExcelSheetName` for formulas if Excel requires a match to an existing predecessor
4. Write letterhead / title / account number
5. Write data band from `DocumentRow` (Date, derived day, Hours, Location, Subject)
6. Apply merge-by-date on columns A–B
7. Write `=SUM(Cstart:Cend)` on totals C; carry-in formula pointing at previous instance's **actual** remaining-cell; E = D − C
8. Write billing formulas from RateCard (never typed numbers for subtotal/VAT/total)
9. Save

Carry-in formula must quote the previous sheet name Excel-style (`'פברואר  2026 '!E31`) using the **real** stored sheet name of the previous instance, whitespace included.

---

### Task 3: Golden tests

`PythonScripts/tests/render/test_xlsx_golden.py`:
- Styles: font name/size, fill, border on a sample of title + header + data cells match golden
- Merges: set equality of merged cell ranges on letterhead/declaration; data-band merges match policy
- Column widths A–F within 0.1 of golden
- `rightToLeft is True`
- Formula strings: totals C is a SUM; D is a cross-sheet ref; E is `D-C` / `D - C`

`PythonScripts/tests/render/test_balance_chain.py`:
- Three months, band lengths 3 / 10 / 1
- Assert `E(n) = D(n) − C(n)` (data_only after Excel is not available — assert **formula structure** and computed values in Python)
- `D(n+1)` references `E(n)` at the actual remaining row of month n

`test_sheet_name_tolerance.py`: predecessor named `'מרץ 2025 '` (trailing space) still produces a formula Excel will resolve.

**P6 done when:** those three test modules pass; commit endpoint writes a file the tests can open.
