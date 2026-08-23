# Gotchas

- **`System.Activities`** in `Lus.Application.csproj` is a legacy Windows-only reference, **unused in code**, made conditional on `Windows_NT`. Don't re-add it unconditionally — it breaks Linux/Docker builds.
- **`global.json`** lives in `src/`, not the repo root. Keep it on SDK 9.x.
- **Cookie `Secure`** must be `false` for localhost/dev (no HTTPS) and `true` in prod. `Domain` empty in dev, `.shiftiz.com` in prod.
- **Org scoping** is per-handler, not EF global filters. New tenant-owned reads must filter by `OrganizationId` explicitly (search handlers auto-inject it).
- **Current-org cache key**: `user_organization_id_{userId}` (EasyCaching). Eviction happens on logout and org change.
- **Two cookies** are issued (idsrv + api) via `CookieAuthSessionService.SignInAsync`. Logout must clear both.
- **Pre-existing NuGet vuln warnings** (IdentityServer4 4.1.2, AutoMapper 12.0.1) — known, not introduced by the upgrade; revisit when replacing IdentityServer4.
- **publish/** output dirs are gitignored; don't commit build artifacts.
- **`src/Lus.UI` is a nested git repo** (yotke/Lus.FE) and is gitignored from Lus.BE. P2/P5 frontend work must be committed there.
- **Local compose builds the API from the repo root** (`context: ..`, root `Dockerfile`) so `PythonScripts/` is in the image. Do not switch back to `src/Dockerfile` without copying scripts.
- **`Caching:ProviderName=redis` still registers EasyCaching as name `default`** so existing `IEasyCachingProvider` injections keep resolving. The flag selects the backend, not the DI name.
- **Workbook series is open-ended** (no year column). The year in the exemplar filename is a label; the balance chain may cross years.
- **Do not `dotnet ef database update` from a laptop.** `appsettings.Development.json` points at production Railway MySQL (`AutoMigrations: false`).
- **Do not `dotnet ef migrations add` until Organization / ProjectTemplate / ProjectTime are back in `ApplicationContext.OnModelCreating`.** Those tables are in the snapshot but currently omitted from the live model; a new migration would try to drop them. Document builder entity configs are already applied — generate `AddDocumentBuilder` only after restoring those three configs (or after a careful model diff).


## AI builders port (2026-08-18)

- **Two Claude sessions were writing this tree concurrently.** Symptom seen: a test file containing
  the SAME namespace+class twice (`DocumentTotalsCalculatorTests`, 3 build errors). If the build
  breaks with CS0101/CS0111 on a file nobody meant to duplicate, suspect a concurrent write before
  suspecting the code. Use `ListAgents` to check for peer sessions.
- **`DatabaseExtensionsTests` asserted a full connection string** and went stale the moment
  `SslMode=None;AllowPublicKeyRetrieval=true;` was added (Railway MySQL uses caching_sha2_password
  over a non-TLS internal link — Pomelo needs both or the handshake fails). Rewritten to parse the
  string and assert the parts, so adding an option no longer breaks unrelated tests.
- **The agent envelope is deserialized, never forwarded.** `AgentEnvelopeParser.Parse<TResult>`
  returns a typed `AgentEnvelopeDto<TResult>`; an endpoint must not `Content(raw, "application/json")`
  the Python stdout — that leaks an untyped contract to every client and to Swagger.
- **`AgentEnvelopeParser` never throws.** Unreadable stdout becomes `envelope_unparseable`,
  `Ok:true` with a null `Result` becomes `empty_result`. A misbehaving agent degrades one turn; it
  must not 500 the request.
- **`EchoResultDto` is pinned to the Python schema by a test** that reads
  `agents/schemas/echo.result.schema.json` at runtime. Hand-synced contracts drift; the schema is
  the source of truth because the runner validates against it before emitting.
- **Migrations run from `Lus.Infrastructure` as BOTH `--project` and `--startup-project`.** That
  works because of `Persistence/ApplicationContextDesignTimeFactory.cs`, which pins
  `MySqlServerVersion 8.0` instead of calling `ServerVersion.AutoDetect` — AutoDetect opens a
  connection, so without the factory no migration can be generated without a live MySQL. The
  factory is EF-tooling-only; runtime behaviour is unchanged. See `docs/MIGRATIONS.md`.
- **`Model_has_no_pending_changes_beyond_the_last_migration` fails = add a migration**, never
  weaken the test. It diffs the DESIGN-TIME model (not `context.Model`, which is read-optimized and
  throws on `Collation`).
- **CIRCULAR-DI LAW: an HTTP interceptor must never constructor-inject an HttpClient-dependent
  service.** Building `HttpClient` resolves `HTTP_INTERCEPTORS`, so the loop throws NG0200 and
  **every HttpClient call in the app dies silently** — it surfaced as "i18n is broken" (raw keys,
  zero requests for `he.json`) on 2026-08-18. Resolve such services lazily via `Injector` inside
  the interceptor. Pinned by `Adapters/Interceptors/interceptor-di.spec.ts`. Note
  `TranslateService` counts — its loader is `TranslateHttpLoader`.
- **`ng test` is blocked by 8 pre-existing spec compile errors** in create-project, data-loader,
  auto-complete and checkbox-input specs. `Adapters/Common/Filters/**` is excluded in
  `tsconfig.spec.json` because it imports a service that no longer exists. See
  `.brain/2026-08-18-ng0200-i18n-outage.md`.
