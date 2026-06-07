# Cookie-based Login (Backend + Frontend)

Luz uses **HTTP-only cookie authentication** for the browser, layered alongside the existing IdentityServer4 + JWT bearer stack (kept for existing token clients). This mirrors the ArmyLuz design.

## Backend

### Schemes
`Lus.Authorization/Authentication/CookieAuthSchemes.cs` defines:
- `IdentityServer` cookie (`<name>_idsrv`)
- `Api` cookie (`<name>`)
- `Smart` policy scheme — routes a request to the right scheme (cookie for browser, bearer for API tokens).

`Startup.cs` registers both cookies + the Smart policy scheme and sets the default authorization policy to require an authenticated user via the API cookie.

### Session service
`Lus.Authorization/Authentication/CookieAuthSessionService.cs` (`ICookieAuthSessionService`):
- `SignInAsync` — issues **both** cookies from a `ClaimsPrincipal` (8h, sliding).
- `SignOutAsync` — clears both cookies + session and evicts the user's org cache entry.

Claims placed in the cookie: user id (`sub`/`NameIdentifier`), name, email, phone, **`security_stamp`** (session invalidation), organization context, and permission/role claims.

### Endpoints (`Lus.Api/Controllers/AuthController.cs`)
- `POST /api/auth/login` — validate credentials → build principal → `SignInAsync` → return auth state.
- `POST /api/auth/logout` — `SignOutAsync`.
- `GET  /api/auth/state` — current auth state (user + org + permissions).
- CSRF token endpoint — double-submit token for state-changing requests.

### CSRF
`Lus.Authorization/Csrf/CsrfTokenService.cs` issues a double-submit token; the SPA echoes it in the `X-XSRF-TOKEN` header. Cookie/header names + domain configured under `Auth:Csrf`.

### Caching
`ICachingService` (EasyCaching wrapper) caches the user→organization mapping and other hot lookups. Default provider in-memory; switch to Redis via `Caching:ProviderName`.

## Frontend (`Lus.UI`)

`src/app/Infrastructure/Services/Auth/auth.service.ts`:
- All auth calls use `withCredentials: true` so the browser sends/receives the cookies.
- Maintains an `authState$` store (`isAuthenticated`, `user`, `currentOrganization`, `organizations`, `roles`, `permissions`).
- `logout()` → `POST /api/auth/logout`; `checkAuthStatus()` → `GET /api/auth/state`; fetches a CSRF token after login.
- Interceptors (`Adapters/Interceptors/auth.interceptors`) attach credentials + CSRF header; specs cover them.

## Config keys (`appsettings*.json`)

```jsonc
"Auth": {
  "Cookie": { "Name": "lus_sid", "Domain": ".shiftiz.com", "Secure": true, "SameSite": "Lax", "SessionTimeoutHours": 8 },
  "Csrf":   { "HeaderName": "X-XSRF-TOKEN", "CookieName": "XSRF-TOKEN", "Domain": ".shiftiz.com", "Secure": true, "SameSite": "Lax" }
}
```
In Development the cookie `Domain` is empty and `Secure` is false (localhost).
