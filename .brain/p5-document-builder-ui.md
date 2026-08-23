# P5 — Document Builder UI

> Parent + spec §4 architecture diagram. Frontend in nested `src/Lus.UI` (commit in Lus.FE).
> Shape ported from ArmyLuz `Activities/OrganizationsManagement/ai-org-builder/` — **spreadsheet canvas, not the 12 org widgets.**

**Goal:** Chat rail, agent ticker, question chips, doc-canvas preview.

**Exit criterion:** Dictating a work-log line renders a new row live on the canvas.

**Depends on:** P2 loading (bypassSpinner), P3 turn endpoint, P4 row_extractor (or P3 stub if sequencing requires a thin "lastUtterance" row first — prefer waiting for P4).

---

### Task 1: Types + API + state services

**Files:**
- `src/app/Infrastructure/Services/documentBuilderService/builder.types.ts`
- `document-builder-api.service.ts` — `POST v1/documents/builder/turn|undo|redo|commit`, `withCredentials`, `bypassSpinner` on turn/commit
- `document-builder-state.service.ts` — sessionId, version, draft, apply ops, connect SignalR `/hub/document-builder`
- `builder-payload.normalizer.ts`

Draft ops applied **once** (HTTP response *or* SignalR `DraftPatched` with same version — ignore duplicate version).

Canvas host law: `:host { display: flex; flex: 1; min-height: 0; }` on the canvas component.

---

### Task 2: Shell UI

**Files:**
- `src/app/Activities/DocumentBuilder/document-builder.component.ts` (shell)
- `doc-canvas/doc-canvas.component.ts` — HTML table, RTL, columns תאריך / יום / שעות / מיקום / נושא, totals row
- chat rail (reuse ArmyLuz `ai-chat-rail-core` mixin if copyable; otherwise a right-side rail with textarea)
- agent ticker (`AgentStatus` → `"doc.row_extractor is thinking…"` / Hebrew copy FE-owned)
- `builder-question-chips` 

Route: `/documents/builder` behind auth guard. Add a nav entry next to projects-manager. Do **not** remove create-project / excel-export.

Upload control: exemplar `.xlsx` → later `POST v1/documents/builder/exemplar` (if P3 didn't add it, add a thin upload endpoint here that stores a file id and calls `doc.template_reader`).

---

### Task 3: Live row from dictation

Type: `5 במרץ, 3 שעות במשרד, התייעצות עם מתכנן התנועה`

Expect: canvas shows a new row, day-of-week filled (`ד` or equivalent), hours 3, version bumped, chips for the next question.

Test: component spec with a fake API returning one AddRow op — canvas row count 0→1.

**P5 done when:** that dictation path works against a running API (manual) and the unit spec is green.
