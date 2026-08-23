# P7 — PDF renderer

> Parent + spec §6.1. There is **no PDF stack to port** — net-new. Lus already has itext7 / GhostScript / Select.HtmlToPdf in the API for other flows; the **document** PDF is a Python page-set renderer of the same draft, because the exemplar PDFs are the print of the sheet.

**Goal:** Same draft → PDF that matches the supplied monthly PDFs.

**Exit criterion:** Output matches the supplied monthly PDFs (visual + structural checks below).

**Depends on:** P6 xlsx renderer (PDF is the print of that sheet).

---

### Task 1: Choose the engine (locked here so implementers don't bike-shed)

**Use LibreOffice/soffice headless to export the rendered xlsx to PDF** if `soffice` can be installed in the image without exploding size. Fallback: `openpyxl` + `reportlab` page-set that copies print area, RTL, A4.

P0 image already has `ghostscript` + `fontconfig` (legacy C# PDF). Prefer:

1. Try `soffice --headless --convert-to pdf` on the P6 xlsx (best fidelity to "print of the sheet").
2. If LibreOffice is too heavy for Railway, implement `PythonScripts/render/pdf_renderer.py` with reportlab, printing the same blocks, RTL, signature lines.

Document the choice in `docs/DOCUMENT_BUILDER.md` when implemented.

Install Hebrew-capable fonts in the image (`fonts-dejavu` or a committed Noto Sans Hebrew). Without this, PDF tests will fail on missing glyphs.

---

### Task 2: `pdf_renderer.py`

Input: path to the P6 xlsx (or the draft JSON + exemplar). Output: `.pdf` bytes.

Print setup inherited from the sheet (`page_setup.orientation`, fit-to-page, rightToLeft).

Filename pattern for download: `DDMMYY01 <client> <month> <year> ח-ן.pdf` (spec §3.1).

---

### Task 3: Tests

- `test_pdf_page_count.py` — one month → 1 page (the exemplars are single-page).
- `test_pdf_hebrew_extract.py` — extracted text contains `דוח ביצוע שעות עבודה`, the client name, and a subject string from the golden draft (pdfminer.six or pypdf).
- Visual: optional screenshot diff against `PythonScripts/tests/golden/01032601-rst-march.pdf` — **non-blocking** if anti-aliasing differs; structural tests are the gate.
- Empty-rate draft must not produce a PDF (commit already blocked in P6; assert renderer is not called).

**P7 done when:** pytest extracts the Hebrew title from the PDF; download endpoint `GET v1/documents/instances/{id}/pdf` returns `application/pdf`.
