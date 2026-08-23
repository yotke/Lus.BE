# Non-Blocking Loading

> Status: **specified 2026-08-18, not yet implemented in Lus.** Ported from ArmyLuz `docs/NON_BLOCKING_LOADING.md`. This is P2 of the AI builders port — bundled because a multi-agent wave behind a blocking overlay is unusable.

## Why

Lus today uses `Adapters/loader` + `blockUI-request` — the **legacy full-screen blocking overlay** ArmyLuz explicitly deleted. Every HTTP request confiscates the screen, with no show-delay (fast requests flash a GIF) and a show/hide race that can leave the app permanently blocked.

Law: **loading must never take the app away from the user.** Indicate activity, don't confiscate the screen.

## Three tiers

| Tier | When | Surface |
|---|---|---|
| 1 Global activity | any tracked HTTP request | mini-toast, pulsing icon + what is loading |
| 2 Region loading | a screen fetching its data | `app-skeleton` shimmer |
| 3 Narrated long op | the builder wave | SignalR `WorkflowProgress` → stage text + honest percent |

## Laws

1. **Nothing blocks the whole screen.** A long operation may cover *its own region* — the rest of the app stays usable.
2. **Fast is invisible.** Requests under 300ms show no indicator. The toast appears after 300ms continuous loading and stays ≥400ms.
3. **Counter, not boolean.** `LoadingService.begin()/end()` are paired in the interceptor's `finalize`. The pending counter cannot leak or go negative.
4. **`LoadingService` never injects anything HTTP-adjacent** (circular-DI law).
5. **`bypassSpinner` HttpContext token** for flows with their own surface (builder turn, commit).
6. **Narration is enhancement-only.** If SignalR is down, the operation still completes off the HTTP response; the user loses the ticker, not the work. Progress emission on the BE is fire-and-forget and may never fail the operation.
7. **Percentages are honest only where a real denominator exists.** Elsewhere the *message* carries the truth, not a fake percent.
8. **FE owns narration copy** (he/en, RTL-safe). BE sends stage keys + structured `Details`.

## What to delete

- `src/Lus.UI/src/app/Adapters/loader/` (component + service that drive the overlay).
- `src/Lus.UI/src/app/Adapters/Interceptors/blockUI-request/` and its registration in `app.module.ts`.

Frontend lives in the nested `yotke/Lus.FE` repo at `src/Lus.UI` (gitignored from the backend repo). P2 lands there.

## What to port (shape, not scheduling copy)

- `LoadingService` with `begin()` / `end()` counter + 300/400ms hysteresis.
- Mini-toast host in `app.component`.
- `app-skeleton` shimmer for heavy screens.
- `bypassSpinner` context token on the HTTP interceptor.
- BE: `IWorkflowProgressService` + `ProgressReporterBase` + stage catalog (P1 plumbing, P2 surfaces it). SignalR already exists in Lus (`CitiesStreetsHub`); the builder hub is added in P0.
