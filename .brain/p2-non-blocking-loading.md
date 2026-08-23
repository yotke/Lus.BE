# P2 — Non-blocking loading (delete the overlay)

> Parent plan + [`docs/NON_BLOCKING_LOADING.md`](../docs/NON_BLOCKING_LOADING.md)
> Frontend lives in the nested repo `src/Lus.UI` (yotke/Lus.FE). **Commit there, not in Lus.BE.**

**Goal:** Replace the full-screen `blockUI-request` overlay with three loading tiers.

**Exit criterion:** No full-screen block anywhere; <300ms requests show nothing.

**Depends on:** P0 hub exists. P1 progress plumbing can be stubbed — tier 3 narration is enhancement-only.

ArmyLuz source (shape, not scheduling widgets):
- `ArmyLuz.UI/src/app/Infrastructure/Services/loading.service.ts` (or similar — search `begin()` / `bypassSpinner`)
- `docs/NON_BLOCKING_LOADING.md` in ArmyLuz
- Delete Lus: `Adapters/loader/**`, `Adapters/Interceptors/blockUI-request/**`

---

### Task 1: LoadingService (counter + hysteresis)

**Files (Lus.UI):**
- Create: `src/app/Infrastructure/Services/loading/loading.service.ts`
- Create: `src/app/Infrastructure/Services/loading/bypass-spinner.token.ts`
- Test: `loading.service.spec.ts`

```ts
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private pending = 0;
  readonly active$ = new BehaviorSubject(false);

  begin(): void { this.pending++; this.schedule(); }
  end(): void { this.pending = Math.max(0, this.pending - 1); this.schedule(); }

  // appear after 300ms continuous; stay ≥400ms once shown
}
```

- [ ] Tests: begin/end cannot go negative; two overlapping requests keep active true until both end; a 100ms request never emits active true.
- [ ] Commit in Lus.FE: `feat: add LoadingService counter with 300/400ms hysteresis`

---

### Task 2: Replace the interceptor

**Files:**
- Delete: `Adapters/loader/` (component + service)
- Delete: `Adapters/Interceptors/blockUI-request/`
- Create: `Adapters/Interceptors/loading/loading.interceptor.ts`
- Modify: `app.module.ts` / `app.component` — remove overlay host, add mini-toast host

Interceptor: `begin()` on intercept, `end()` in `finalize`. Skip when `HttpContext` has `bypassSpinner`. Never inject HttpClient into `LoadingService`.

Mini-toast: pulsing icon + `common.loading` (Hebrew). No full-screen backdrop. No CDK overlay.

- [ ] Manual: a fast GET shows nothing; a throttled GET shows the toast then hides.
- [ ] Commit: `feat: replace blocking overlay with mini-toast loading`

---

### Task 3: `app-skeleton` (tier 2)

**Files:**
- Create: `src/app/Adapters/skeleton/skeleton.component.ts` (`app-skeleton`)
- Use on `projects-manager` list while fetching (the heaviest current screen). Do not restyle every page.

- [ ] Commit: `feat: add app-skeleton shimmer for region loading`

---

### Task 4: bypassSpinner on future builder calls

Export `bypassSpinner` context. P5's `document-builder-api.service` will set it on `turn` / `commit`. In P2, add a comment + unit test that the interceptor respects the token.

**P2 done when:** grep finds no `blockUI` / `loader.component`; a 200ms request in the spec stays invisible; a 1s request shows the mini-toast only.
