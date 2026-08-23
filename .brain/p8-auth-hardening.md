# P8 — Auth hardening

> Parent + [`docs/AUTH_HARDENING.md`](../docs/AUTH_HARDENING.md). Spec D4.

**Goal:** `[Permission("documents.build")]` enforced; security stamp invalidation works.

**Exit criterion:** those two facts, plus headers + correlation id.

**Depends on:** P3 controller exists. Do this after the builder is callable so the permission has a real target.

**Not in scope:** tenant middleware, subdomain redirect, choose-organization, apex-only routing.

Lus already has cookie auth, CSRF, `/api/auth/state`, a `PermissionPolicyProvider`, and `security_stamp` on the cookie. P8 deepens them; it does not replace cookie login.

---

### Task 1: Claim-backed `[Permission]`

ArmyLuz source: `PermissionAttribute`, `PermissionRuleDefinition`.

- Seed permission `documents.build` on ORGANIZATION_OWNER, ORGANIZATION_ADMIN, MANAGER, SITEADMIN, DEVELOPER. Not EMPLOYEE.
- Handler succeeds when the principal has that permission claim (or SITEADMIN/DEVELOPER bypass — already present).
- Put `[Permission("documents.build")]` on `DocumentBuilderController`.

Tests:
- EMPLOYEE cookie → `POST v1/documents/builder/turn` is 403
- ORGANIZATION_ADMIN cookie → 200 (or 400 from validation, **not** 403)

---

### Task 2: SecurityStampValidator

Port ArmyLuz `SecurityStampValidator` + store. On password change / role change, bump stamp; next request with the old cookie is 401.

Test: sign in, bump stamp in DB, next `GET /api/auth/state` is 401.

---

### Task 3: Middleware + Apple + reauth

- `SecurityHeadersMiddleware` — nosniff, DENY frame, referrer policy. Test: response headers present.
- `CorrelationIdMiddleware` — `X-Correlation-Id` in and out; include on SignalR error events when P1 sender is used.
- `ReauthTokenService` — commit/render can require a fresh reauth token later; ship the service + a test that an expired token is rejected. Controller attribute can wait if it blocks P6; prefer `[Reauth]` on commit only.
- `Adapters/AppleSignin` — port only if `environment.appleSignInEnabled` will be used; otherwise a stub behind a false flag. Do not block P8 exit on Apple App Store setup.

---

### Task 4: AuthState completeness

Align `/api/auth/state` with `IAuthStateResolver` so the FE receives `permissions: ["documents.build", …]`. Lus.UI `auth.service.ts` already has a permissions field — populate it for real.

**P8 done when:** EMPLOYEE 403 on the builder; stamp invalidation test green; security headers on `/health` or a documented exemption if health stays header-light.
