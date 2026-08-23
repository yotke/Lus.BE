# .brain

Working memory for agents/devs on the Luz project: decision logs, session notes, and gotchas that aren't obvious from the code. Keep entries short and dated. Durable architecture lives in `/docs`; `.brain` is the running log of *why* and *what's in flight*.

## Index
- [`2026-08-18-armyluz-to-lus-port-research.md`](./2026-08-18-armyluz-to-lus-port-research.md) — inventory of ArmyLuz vs Lus; exemplar workbook analysis.
- [`p0-redis-signalr-python.md`](./p0-redis-signalr-python.md) — P0: Redis + DocumentBuilder hub + Python in the image.
- [`p1-kernel-adapter-runner.md`](./p1-kernel-adapter-runner.md) — P1: kernel + `PythonScriptsAdapter` + `doc.echo`.
- [`p2-non-blocking-loading.md`](./p2-non-blocking-loading.md) — P2: delete blocking overlay (Lus.FE).
- [`p3-documents-builder-backend.md`](./p3-documents-builder-backend.md) — P3: entities, DraftPatcher, turn.
- [`p4-doc-agents-template-reader.md`](./p4-doc-agents-template-reader.md) — P4: `doc.*` agents + exemplar parse.
- [`p5-document-builder-ui.md`](./p5-document-builder-ui.md) — P5: chat rail + canvas (Lus.FE).
- [`p6-xlsx-renderer.md`](./p6-xlsx-renderer.md) — P6: openpyxl round-trip + golden tests.
- [`p7-pdf-renderer.md`](./p7-pdf-renderer.md) — P7: PDF as print of the sheet.
- [`p8-auth-hardening.md`](./p8-auth-hardening.md) — P8: `[Permission("documents.build")]` + security stamp.
- [`2026-06-07-net9-cookie-auth.md`](./2026-06-07-net9-cookie-auth.md) — .NET 9 upgrade, cookie auth, Railway/Docker, shiftiz.com.
- [`2026-06-07-org-projections-search.md`](./2026-06-07-org-projections-search.md) — org scoping + projections + filter engine port.
- [`2026-08-18-strong-typing-and-test-pass.md`](./2026-08-18-strong-typing-and-test-pass.md) — typed-envelope API rule, schema drift guard, the two fixes that made the suite green (58/58).
- [`2026-08-18-ng0200-i18n-outage.md`](./2026-08-18-ng0200-i18n-outage.md) — the circular-DI law: interceptors must never constructor-inject HttpClient-dependent services.
- [`gotchas.md`](./gotchas.md) — sharp edges to remember.

Spec: [`docs/superpowers/specs/2026-08-18-ai-builders-port-design.md`](../docs/superpowers/specs/2026-08-18-ai-builders-port-design.md)
Master plan: [`docs/superpowers/plans/2026-08-18-ai-builders-port.md`](../docs/superpowers/plans/2026-08-18-ai-builders-port.md)
