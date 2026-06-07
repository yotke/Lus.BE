# Gotchas

- **`System.Activities`** in `Lus.Application.csproj` is a legacy Windows-only reference, **unused in code**, made conditional on `Windows_NT`. Don't re-add it unconditionally — it breaks Linux/Docker builds.
- **`global.json`** lives in `src/`, not the repo root. Keep it on SDK 9.x.
- **Cookie `Secure`** must be `false` for localhost/dev (no HTTPS) and `true` in prod. `Domain` empty in dev, `.shiftiz.com` in prod.
- **Org scoping** is per-handler, not EF global filters. New tenant-owned reads must filter by `OrganizationId` explicitly (search handlers auto-inject it).
- **Current-org cache key**: `user_organization_id_{userId}` (EasyCaching). Eviction happens on logout and org change.
- **Two cookies** are issued (idsrv + api) via `CookieAuthSessionService.SignInAsync`. Logout must clear both.
- **Pre-existing NuGet vuln warnings** (IdentityServer4 4.1.2, AutoMapper 12.0.1) — known, not introduced by the upgrade; revisit when replacing IdentityServer4.
- **publish/** output dirs are gitignored; don't commit build artifacts.
