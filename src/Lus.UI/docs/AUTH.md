# Authentication (Cookie + CSRF)

The app uses **cookie-based** authentication against the Lus.Api backend. No JWT
is stored in the browser; the session lives in an HttpOnly cookie set by the API.

## Flow

1. **Login** — credentials are posted to the identity/token endpoint. The API
   responds with a `Set-Cookie` HttpOnly session cookie (e.g. `lus_sid`).
2. **CSRF** — `CsrfService.getToken()` issues `GET /csrf-token`, which returns a
   token and sets the `XSRF-TOKEN` cookie.
3. **State-changing requests** — the `CsrfInterceptor` attaches the
   `X-CSRF-Token` header to `POST/PUT/PATCH/DELETE`. It skips `/csrf-token` and
   `/connect/` endpoints and retries once after refreshing the token on 400/403.
4. **Every request** — the auth interceptor adds `withCredentials: true` so the
   session cookie is sent cross-origin to the API.
5. **Auth state** — `AuthService` keeps a `BehaviorSubject` of the current user,
   organizations, roles and permissions, hydrated from `GET /api/auth/state`.
6. **Logout** — `POST /api/auth/logout` clears the server session; the client
   clears its in-memory state and `sessionStorage`.

## Cookies & domain

For `shiftiz.com` the backend sets the cookie `Domain=.shiftiz.com` so the
session is shared across subdomains (`app.`, `api.`, `www.`). Locally the domain
is empty and `Secure=false`.

| Setting | Local | Production (shiftiz.com) |
|---------|-------|--------------------------|
| `Auth:Cookie:Domain` | `""` | `.shiftiz.com` |
| `Auth:Cookie:Secure` | `false` | `true` |
| `Auth:Cookie:SameSite` | `Lax` | `Lax` |

## Relevant files

- `src/app/Infrastructure/Services/Auth/auth.service.ts`
- `src/app/Infrastructure/Services/Auth/csrf.service.ts`
- `src/app/Adapters/Interceptors/auth.interceptors/*`
