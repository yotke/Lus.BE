# Auth Hardening

> Status: **specified 2026-08-18 as P8.** Spec decision **D4**: auth hardening only, no org/tenancy work. Lus stays per-handler org scoping.

## What Lus already has

Cookie auth + CSRF + `IUserAccessor`, Google sign-in, `GET /api/auth/state`, `security_stamp` claim on the cookie, and a `PermissionPolicyProvider` / `PermissionRequirementHandler` pair. See [`AUTH_COOKIE_LOGIN.md`](./AUTH_COOKIE_LOGIN.md).

The permission handler today only understands a small hardcoded map of admin-role policies. It does **not** yet enforce arbitrary `[Permission("documents.build")]` claims.

## What to port from ArmyLuz.Authorization

| Piece | Why |
|---|---|
| `PermissionAttribute` + claim-backed `PermissionRequirementHandler` | First use: `[Permission("documents.build")]` on `DocumentBuilderController`. |
| `PermissionRuleDefinition` | Declarative permission catalog, not a switch in the handler. |
| `SecurityStampValidator` + `SecurityStampStore` | Server-side session invalidation on password/role change. Lus already puts `security_stamp` on the cookie; it does not yet validate it on each request. |
| `IAuthStateService` / `IAuthStateResolver` | One endpoint the FE asks "who am I, what can I do, where do I land" — Lus has a thinner `/api/auth/state`. |
| `ReauthTokenService` | Short-lived step-up token for sensitive actions (commit / render). |
| `SecurityHeadersMiddleware` | CSP / X-Frame / nosniff. |
| `CorrelationIdMiddleware` | Request id on every log line and SignalR event. |
| `Adapters/AppleSignin` | Lus has Google, lacks Apple. |
| `PersistentSecurityAuditService` | Lus has `UserLoginAttemptsAuditService` already; keep it, add the persistent sibling. |

## What is deliberately not ported

| Piece | Why |
|---|---|
| `TenantResolutionMiddleware` | Lus is not subdomain-per-org. |
| `SubdomainRedirectService` | Same. |
| Choose-organization / apex-only routing | Lus keeps per-handler `OrganizationId` from `IUserAccessor` cache. |

## First permission

```csharp
[Permission("documents.build")]
public class DocumentBuilderController : ControllerBase { … }
```

SITEADMIN / DEVELOPER bypass remains (existing handler already grants those). ORGANIZATION_OWNER / ORGANIZATION_ADMIN / MANAGER receive `documents.build` via seed. EMPLOYEE does not.

## Tests

- `[Permission("documents.build")]` returns 403 for an EMPLOYEE cookie and 200 for an org-admin cookie.
- Changing the security stamp (password reset) invalidates the existing cookie on the next request.
- Security headers present on API responses.
- Correlation id echoed on error envelopes.
