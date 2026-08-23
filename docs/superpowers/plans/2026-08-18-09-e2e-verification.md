# E2E Verification Harness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up a Playwright e2e harness in Lus — ported from ArmyLuz's proven setup — that verifies today's flows before anything moves, and then verifies each ported subsystem as it lands. Every migration plan gains an executable "did we break the app?" answer.

**Architecture:** ArmyLuz's e2e architecture ported: `playwright.config.ts` with an auth-setup project that logs in once and saves storage state, page objects per screen, fixture users in a gitignored env file. The first deliverable is a **baseline suite over the app as it exists now** (login, projects manager, create project, data loader, validator, Excel export, history) — written *before* the migration plans execute, so regressions caused by plans 01–08 are caught by tests that predate them. Interactive checking with the Claude-in-Chrome extension complements the suite for exploratory verification; the suite is the gate, the extension is the magnifier.

**Tech Stack:** `@playwright/test` ^1.58, TypeScript, page-object pattern, dotenv. No new backend code.

**Spec:** [`docs/superpowers/specs/2026-08-18-ai-builders-port-design.md`](../specs/2026-08-18-ai-builders-port-design.md) §10 (testing).

## Global Constraints

- **The baseline comes first.** No migration plan's execution should start before Task 3 is green — that is the whole point of the harness.
- **Page objects, not inline selectors.** Every screen interaction goes through `e2e/page-objects/*.po.ts`; specs read as flows.
- **Credentials never enter git.** `.env.e2e` is gitignored; `e2e-users.json` holds only local-dev fixture users that exist solely in a dev database.
- **`waitForLoadState('domcontentloaded')`, not `networkidle`** — ArmyLuz's scar: pages hosting third-party iframes (Google sign-in, reCAPTCHA) keep the network busy forever, so `networkidle` never settles. The real readiness gate is a visible element.
- **Hebrew is the default UI language.** Assertions match against Hebrew copy or, better, against structure (roles, test ids, table shape) rather than text.
- e2e lives in `src/Lus.UI/e2e/`; runs against `http://localhost:4200` (dev server) with the API on `:8080`.

## Starting state (verified 2026-08-18)

- Lus has **zero e2e tests** — only 4 xUnit backend classes and scattered Karma specs.
- ArmyLuz has the full harness: `playwright.config.ts` (auth-setup project → storage state, html+list reporters, trace/screenshot/video on failure), 25+ spec files, 20 page objects, `fixtures/auth.setup.ts`, `.env.e2e` convention, `@playwright/test` ^1.58.2.
- Lus screens to cover: `login`, `register`, `projects-manager`, `create-project`, `data-loader`, `project-validator`, `excel-export`, `history-projects`, `legal`.
- The Claude-in-Chrome extension is available in this environment for interactive verification sessions; it is **not** part of CI.

---

### Task 1: Port the Playwright skeleton

**Files:**
- Create: `src/Lus.UI/playwright.config.ts`
- Create: `src/Lus.UI/e2e/tsconfig.json`
- Create: `src/Lus.UI/e2e/fixtures/auth.setup.ts`
- Create: `src/Lus.UI/e2e/fixtures/e2e-users.json`
- Create: `src/Lus.UI/.env.e2e.example`
- Modify: `src/Lus.UI/package.json` (devDependency + scripts)
- Modify: `src/Lus.UI/.gitignore`

**Interfaces:**
- Consumes: nothing — this is the foundation.
- Produces: `npm run test:e2e` executing an authenticated Playwright suite; `e2e/.auth/user.json` storage state produced once per run by the `setup` project; every later spec declares `use: { storageState: AUTH_FILE }` via the shared project config.

- [ ] **Step 1: Install Playwright**

```bash
cd src/Lus.UI
npm install --save-dev @playwright/test@^1.58 dotenv
npx playwright install chromium
```

- [ ] **Step 2: Write the config**

Create `src/Lus.UI/playwright.config.ts` — ArmyLuz's config adapted to Lus's routes:

```typescript
import { defineConfig, devices } from '@playwright/test';
import dotenv from 'dotenv';
import path from 'path';

// Credentials from .env.e2e (gitignored). Create it from .env.e2e.example.
dotenv.config({ path: path.resolve(__dirname, '.env.e2e'), quiet: true } as any);

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: process.env['CI'] ? 1 : undefined,
  reporter: [['html', { outputFolder: 'playwright-report' }], ['list']],

  use: {
    baseURL: process.env['PLAYWRIGHT_BASE_URL'] || 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },

  projects: [
    // Logs in once, saves storage state for every other project.
    { name: 'setup', testMatch: /auth\.setup\.ts/ },
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'e2e/.auth/user.json',
      },
      dependencies: ['setup'],
    },
  ],
});
```

- [ ] **Step 3: Write the auth setup**

Create `src/Lus.UI/e2e/fixtures/auth.setup.ts`:

```typescript
import { test as setup } from '@playwright/test';
import fs from 'fs';
import path from 'path';

/**
 * Runs once before the suite: logs in through the real login form and saves
 * cookie storage state to e2e/.auth/user.json for every spec to reuse.
 *
 * Credentials resolve in order: .env.e2e → e2e-users.json fixture → error.
 */
const AUTH_FILE = path.join(__dirname, '../.auth/user.json');
const E2E_USERS_FILE = path.join(__dirname, 'e2e-users.json');

function loadFixtureUser(): { username: string; password: string } {
  const parsed = JSON.parse(fs.readFileSync(E2E_USERS_FILE, 'utf8')) as {
    defaultUser?: string;
    users?: Record<string, { username?: string; password?: string }>;
  };
  const key = process.env['E2E_USER'] || parsed.defaultUser || 'dev';
  const selected = parsed.users?.[key] || parsed.users?.['dev'];
  return { username: selected?.username ?? '', password: selected?.password ?? '' };
}

setup('authenticate', async ({ page }) => {
  const fixture = loadFixtureUser();
  const username = process.env['E2E_USERNAME'] || fixture.username;
  const password = process.env['E2E_PASSWORD'] || fixture.password;

  if (!username || !password) {
    throw new Error(
      '[auth.setup] Missing credentials. Create src/Lus.UI/.env.e2e with:\n' +
      '  E2E_USERNAME=you@example.com\n  E2E_PASSWORD=YourPassword\n',
    );
  }

  await page.goto('/login');
  // NOT networkidle: third-party iframes (Google sign-in / reCAPTCHA) keep the
  // network busy forever, so networkidle never settles. The visible input is the gate.
  await page.waitForLoadState('domcontentloaded');

  const usernameInput = page
    .locator('input[type="email"], input[name="email"], input[formcontrolname="email"], input[name="username"]')
    .first();
  await usernameInput.waitFor({ state: 'visible', timeout: 15_000 });
  await usernameInput.fill(username);

  const passwordInput = page
    .locator('input[type="password"], input[formcontrolname="password"]')
    .first();
  await passwordInput.fill(password);

  await page.locator('button[type="submit"], button:has-text("התחבר")').first().click();

  // Logged in == we left /login. Assert on the URL, not on copy.
  await page.waitForURL((url) => !url.pathname.includes('login'), { timeout: 30_000 });

  fs.mkdirSync(path.dirname(AUTH_FILE), { recursive: true });
  await page.context().storageState({ path: AUTH_FILE });
});
```

The selector lists are a starting point. In Step 6 you will run this against the real login page
and tighten them to what actually matches — do not leave a selector in place that Step 6 never saw
succeed.

- [ ] **Step 4: Create the fixtures and env example**

Create `src/Lus.UI/e2e/fixtures/e2e-users.json` (local-dev users only — never production
credentials):

```json
{
  "defaultUser": "dev",
  "users": {
    "dev": { "username": "", "password": "" }
  }
}
```

Create `src/Lus.UI/.env.e2e.example`:

```
# Copy to .env.e2e (gitignored) and fill in a LOCAL-DEV user.
E2E_USERNAME=
E2E_PASSWORD=
# PLAYWRIGHT_BASE_URL=http://localhost:4200
```

Create `src/Lus.UI/e2e/tsconfig.json`:

```json
{
  "extends": "../tsconfig.json",
  "compilerOptions": {
    "types": ["node", "@playwright/test"],
    "module": "commonjs",
    "esModuleInterop": true
  },
  "include": ["**/*.ts"]
}
```

- [ ] **Step 5: Gitignore the secrets and artifacts**

Append to `src/Lus.UI/.gitignore`:

```
.env.e2e
e2e/.auth/
playwright-report/
test-results/
```

Add the scripts to `package.json`:

```json
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:e2e:headed": "playwright test --headed",
    "test:e2e:report": "playwright show-report"
```

- [ ] **Step 6: Prove the auth setup against the running app**

Start the stack (API + UI), create/confirm a dev user, fill `.env.e2e`, then:

```bash
npx playwright test --project=setup --headed
```

Expected: the browser logs in and `e2e/.auth/user.json` appears. Fix the selectors now if the login
form differs — this is the step where guesses become verified selectors. Commit only selectors this
step saw work.

- [ ] **Step 7: Commit**

```bash
cd /Users/onecity/Desktop/Yotam/Lus
git add src/Lus.UI/playwright.config.ts src/Lus.UI/e2e src/Lus.UI/.env.e2e.example \
        src/Lus.UI/.gitignore src/Lus.UI/package.json src/Lus.UI/package-lock.json
git commit -m "test(e2e): port the playwright harness skeleton from ArmyLuz"
```

---

### Task 2: Page objects for the existing screens

**Files:**
- Create: `src/Lus.UI/e2e/page-objects/login.po.ts`
- Create: `src/Lus.UI/e2e/page-objects/projects-manager.po.ts`
- Create: `src/Lus.UI/e2e/page-objects/create-project.po.ts`
- Create: `src/Lus.UI/e2e/page-objects/data-loader.po.ts`
- Create: `src/Lus.UI/e2e/page-objects/project-validator.po.ts`
- Create: `src/Lus.UI/e2e/page-objects/excel-export.po.ts`
- Create: `src/Lus.UI/e2e/page-objects/history-projects.po.ts`

**Interfaces:**
- Consumes: the harness from Task 1.
- Produces: one PO class per screen, each `constructor(readonly page: Page)` exposing `Locator` fields and flow methods (`goto()`, plus screen-specific actions). Task 3's specs consume only these.

- [ ] **Step 1: Inventory each screen's actual DOM**

Page objects written blind are fiction. For each screen, read the template and note the stable
hooks:

```bash
cd src/Lus.UI
for f in login projects-manager create-project data-loader project-validator excel-export history-projects; do
  echo "=== $f ==="
  find src/app/Activities -path "*$f*" -name "*.component.html" \
    -exec grep -oE 'class="[^"]+"|routerLink="[^"]+"|formcontrolname="[^"]+"|data-testid="[^"]+"' {} \; | sort -u | head -25
done
```

Also record the routes:

```bash
grep -nE "path:" src/app/app-routing.module.ts | head -30
```

- [ ] **Step 2: Write the page objects**

One class per file, following ArmyLuz's pattern exactly (Locator fields initialised in the
constructor, async flow methods, no assertions except in explicit `expect*` helpers). Example shape
for `login.po.ts` — repeat the pattern for every screen using the selectors found in Step 1:

```typescript
import { expect, Locator, Page } from '@playwright/test';

export class LoginPO {
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitBtn: Locator;
  readonly errorMessage: Locator;

  constructor(readonly page: Page) {
    // Replace with the selectors Step 1 actually found:
    this.emailInput = page.locator('input[formcontrolname="email"]').first();
    this.passwordInput = page.locator('input[formcontrolname="password"]').first();
    this.submitBtn = page.locator('button[type="submit"]').first();
    this.errorMessage = page.locator('.error, .mat-mdc-form-field-error').first();
  }

  async goto() {
    await this.page.goto('/login');
    await this.page.waitForLoadState('domcontentloaded');
    await this.emailInput.waitFor({ state: 'visible' });
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitBtn.click();
  }

  async expectLoggedIn() {
    await this.page.waitForURL((url) => !url.pathname.includes('login'));
  }
}
```

For `excel-export.po.ts`, the key flow method wraps the download:

```typescript
  /** Triggers the export and resolves with the downloaded file's suggested name. */
  async exportAndWaitForDownload(): Promise<string> {
    const downloadPromise = this.page.waitForEvent('download');
    await this.exportBtn.click();
    const download = await downloadPromise;
    return download.suggestedFilename();
  }
```

- [ ] **Step 3: Compile-check**

```bash
npx tsc -p e2e/tsconfig.json --noEmit
```

Expected: clean.

- [ ] **Step 4: Commit**

```bash
cd /Users/onecity/Desktop/Yotam/Lus
git add src/Lus.UI/e2e/page-objects
git commit -m "test(e2e): add page objects for the existing screens"
```

---

### Task 3: The baseline suite — pin today's behaviour

**Files:**
- Create: `src/Lus.UI/e2e/tests/baseline-auth.spec.ts`
- Create: `src/Lus.UI/e2e/tests/baseline-projects.spec.ts`
- Create: `src/Lus.UI/e2e/tests/baseline-excel-export.spec.ts`
- Create: `src/Lus.UI/e2e/tests/baseline-navigation.spec.ts`

**Interfaces:**
- Consumes: the page objects from Task 2.
- Produces: a green suite that every migration plan (01, 02, 07, 08) must keep green. This is the harness's reason to exist.

These specs assert on **structure and behaviour, not copy**: URLs, table row counts, download
events, element visibility. Hebrew copy changes must not break the baseline.

- [ ] **Step 1: Write the auth baseline**

Create `src/Lus.UI/e2e/tests/baseline-auth.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';
import { LoginPO } from '../page-objects/login.po';

// This spec runs UNAUTHENTICATED — it tests the login flow itself.
test.use({ storageState: { cookies: [], origins: [] } });

test.describe('baseline: authentication', () => {
  test('unauthenticated visit to a protected route lands on login', async ({ page }) => {
    await page.goto('/projects-manager');
    await page.waitForURL((url) => url.pathname.includes('login'));
  });

  test('wrong credentials stay on login and surface an error', async ({ page }) => {
    const login = new LoginPO(page);
    await login.goto();
    await login.login('nobody@nowhere.test', 'WrongPassword1!');
    await expect(login.errorMessage).toBeVisible({ timeout: 10_000 });
    expect(page.url()).toContain('login');
  });

  test('valid credentials reach the app', async ({ page }) => {
    const login = new LoginPO(page);
    await login.goto();
    await login.login(process.env['E2E_USERNAME']!, process.env['E2E_PASSWORD']!);
    await login.expectLoggedIn();
  });
});
```

- [ ] **Step 2: Write the projects baseline**

Create `src/Lus.UI/e2e/tests/baseline-projects.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';
import { ProjectsManagerPO } from '../page-objects/projects-manager.po';
import { CreateProjectPO } from '../page-objects/create-project.po';

test.describe('baseline: projects', () => {
  test('projects manager lists projects', async ({ page }) => {
    const projects = new ProjectsManagerPO(page);
    await projects.goto();
    await expect(projects.projectsTable).toBeVisible();
  });

  test('create-project round trip', async ({ page }) => {
    const create = new CreateProjectPO(page);
    const projects = new ProjectsManagerPO(page);
    const name = `e2e-baseline-${Date.now()}`;

    await create.goto();
    await create.fillMinimalProject(name);
    await create.submit();

    await projects.goto();
    await projects.expectProjectVisible(name);

    // Leave no residue: the suite must be re-runnable against the same DB.
    await projects.deleteProject(name);
  });
});
```

(`fillMinimalProject`, `expectProjectVisible`, `deleteProject` are flow methods on the Task 2 page
objects — implement them there against the real form fields found in Task 2 Step 1. If the UI has
no delete affordance, mark the test `test.fixme()` with a comment naming the cleanup gap rather
than leaving residue silently.)

- [ ] **Step 3: Write the Excel export baseline**

Create `src/Lus.UI/e2e/tests/baseline-excel-export.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';
import { HistoryProjectsPO } from '../page-objects/history-projects.po';
import { ExcelExportPO } from '../page-objects/excel-export.po';

/**
 * Pins the LEGACY export path (xlsx-js-style, browser-side). The document builder
 * replaces the data-entry that feeds it — this flow itself must keep working
 * unchanged throughout the migration (spec §1.1: excel-export stays frozen).
 */
test.describe('baseline: excel export', () => {
  test('exporting a project produces an .xlsx download', async ({ page }) => {
    const history = new HistoryProjectsPO(page);
    await history.goto();
    await history.openFirstProject();

    const exportPo = new ExcelExportPO(page);
    const filename = await exportPo.exportAndWaitForDownload();
    expect(filename).toMatch(/\.xlsx$/);
  });
});
```

- [ ] **Step 4: Write the navigation baseline**

Create `src/Lus.UI/e2e/tests/baseline-navigation.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';

/**
 * Every routed screen renders without a client-side error. Cheap, broad, and the
 * first thing the Angular 18->20 upgrade (plan 07) can break.
 */
const ROUTES = [
  '/projects-manager',
  '/create-project',
  '/data-loader',
  '/project-validator',
  '/history-projects',
];

test.describe('baseline: navigation', () => {
  for (const route of ROUTES) {
    test(`${route} renders without console errors`, async ({ page }) => {
      const errors: string[] = [];
      page.on('pageerror', (e) => errors.push(e.message));
      page.on('console', (msg) => {
        if (msg.type() === 'error') errors.push(msg.text());
      });

      await page.goto(route);
      await page.waitForLoadState('domcontentloaded');
      // The app shell must be present and Angular must have bootstrapped.
      await expect(page.locator('app-root')).not.toBeEmpty();

      const fatal = errors.filter(
        (e) => !e.includes('favicon') && !e.includes('Failed to load resource'),
      );
      expect(fatal, `console errors on ${route}:\n${fatal.join('\n')}`).toEqual([]);
    });
  }
});
```

Adjust `ROUTES` to the actual paths recorded in Task 2 Step 1 — a route that 404s into a redirect
still passes `domcontentloaded`, so verify each entry is a real route.

- [ ] **Step 5: Run the suite until green**

```bash
cd src/Lus.UI
npm run test:e2e
```

Iterate on selectors until green. Every failure at this stage is either a wrong selector (fix the
page object) or a real pre-existing bug (record it in `.brain/gotchas.md`, mark the spec
`test.fixme()` with the bug reference — the baseline documents reality, it does not fix the app).

- [ ] **Step 6: Commit**

```bash
cd /Users/onecity/Desktop/Yotam/Lus
git add src/Lus.UI/e2e
git commit -m "test(e2e): pin baseline behaviour of the existing app"
```

---

### Task 4: Per-plan verification specs (grow with the port)

**Files:**
- Create: `src/Lus.UI/e2e/tests/ported-loading.spec.ts` (lands with plan 02)
- Create: `src/Lus.UI/e2e/tests/ported-agent-bridge.spec.ts` (lands with plan 01)
- Create: `docs/superpowers/plans/notes/e2e-per-plan-map.md` (now)

**Interfaces:**
- Consumes: the harness; the features as their plans land.
- Produces: a written mapping — which spec verifies which migration plan — and the first two specs, committed alongside their plans' execution (not before: a spec for a feature that doesn't exist yet cannot go green and would sit red in the suite).

- [ ] **Step 1: Write the map**

Create `docs/superpowers/plans/notes/e2e-per-plan-map.md`:

```markdown
# E2E verification map — which spec gates which plan

| Plan | Verifying spec(s) | What it asserts |
|---|---|---|
| baseline (this plan) | `baseline-*.spec.ts` | today's app, pre-migration |
| 01 python-agent-bridge | `ported-agent-bridge.spec.ts` | `/v1/diagnostics/agents/echo` round-trips Hebrew through the real API |
| 07 angular-18-to-20 | the whole baseline suite | zero regressions across both majors |
| 02 non-blocking-loading | `ported-loading.spec.ts` | no full-screen blocking overlay; <300ms requests show no indicator |
| 08 i18n-expansion | `baseline-navigation.spec.ts` + a locale-switch spec | switching language flips `dir`; no raw keys rendered |
| 03 document-builder-backend | (plan 03 will define API-level specs) | turn endpoint applies patches |
| 04 document-builder-frontend | (plan 04 will define canvas specs) | chat → row appears on canvas |
| 05 document-renderers | download spec: exported file opens and matches golden | |
| 06 auth-hardening | `baseline-auth.spec.ts` + permission-denial spec | RBAC 403s render properly |

Rule: **a migration plan is done only when its row here is green.** The baseline suite runs on
every plan regardless.
```

- [ ] **Step 2: Write the agent-bridge spec (executes together with plan 01)**

Create `src/Lus.UI/e2e/tests/ported-agent-bridge.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';

/**
 * Verifies plan 01 end-to-end THROUGH THE BROWSER's authenticated session:
 * the diagnostics endpoint spawns the real python and Hebrew survives.
 * Skipped automatically until the endpoint exists.
 */
test.describe('ported: python agent bridge', () => {
  test('echo agent round-trips hebrew via the api', async ({ page, request }) => {
    await page.goto('/');
    const response = await page.request.get(
      '/v1/diagnostics/agents/echo?text=' + encodeURIComponent('שלום עולם'),
    );

    test.skip(response.status() === 404, 'plan 01 not yet deployed');

    expect(response.ok()).toBeTruthy();
    const body = await response.json();
    expect(body.ok).toBe(true);
    expect(body.echoed).toBe('שלום עולם');
    expect(body.lang).toBe('he');
  });
});
```

Note the pattern: **the spec self-skips while its feature is absent** (404 → skip). This lets
per-plan specs merge early without sitting red, and flip to enforcing the moment the feature lands.

- [ ] **Step 3: Write the loading spec (executes together with plan 02)**

Create `src/Lus.UI/e2e/tests/ported-loading.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';
import { ProjectsManagerPO } from '../page-objects/projects-manager.po';

/**
 * Verifies plan 02's loading laws in the real browser:
 *   1. Nothing blocks the whole screen.
 *   2. Fast is invisible (<300ms shows no indicator).
 * Self-skips while the legacy blocking overlay is still the active implementation.
 */
test.describe('ported: non-blocking loading', () => {
  test('no full-screen blocking overlay during navigation', async ({ page }) => {
    const projects = new ProjectsManagerPO(page);
    await projects.goto();

    const legacyOverlay = page.locator('.cdk-overlay-backdrop.loader-backdrop, app-loader .overlay');
    test.skip(await legacyOverlay.count() > 0, 'plan 02 not yet executed — legacy loader present');

    // Navigate and assert the app stays interactive: the nav element must remain
    // clickable during the route's data load.
    await page.goto('/history-projects');
    const blocked = await page.evaluate(() => {
      const el = document.elementFromPoint(window.innerWidth / 2, 40);
      return el?.closest('.cdk-overlay-backdrop') != null;
    });
    expect(blocked, 'a full-screen backdrop is intercepting clicks').toBe(false);
  });

  test('fast requests show no loading indicator', async ({ page }) => {
    const projects = new ProjectsManagerPO(page);

    const indicatorAppearances: number[] = [];
    await page.exposeFunction('recordIndicator', (t: number) => indicatorAppearances.push(t));
    await page.addInitScript(() => {
      const observer = new MutationObserver(() => {
        const indicator = document.querySelector('.loading-toast, .top-loading-bar, app-loader');
        if (indicator && (indicator as HTMLElement).offsetParent !== null) {
          (window as any).recordIndicator(performance.now());
        }
      });
      observer.observe(document.documentElement, { childList: true, subtree: true });
    });

    await projects.goto();
    test.skip(await page.locator('app-loader').count() > 0, 'plan 02 not yet executed');

    // A cached, fast route change should register no indicator at all.
    indicatorAppearances.length = 0;
    await page.goto('/projects-manager');
    await page.waitForLoadState('domcontentloaded');
    expect(indicatorAppearances.length).toBe(0);
  });
});
```

- [ ] **Step 4: Run — expect skips, not failures**

```bash
cd src/Lus.UI && npm run test:e2e
```

Expected: baseline green; the two `ported:` specs **skipped** (their features don't exist yet).
That's the designed state at this point in the migration.

- [ ] **Step 5: Commit**

```bash
cd /Users/onecity/Desktop/Yotam/Lus
git add src/Lus.UI/e2e docs/superpowers/plans/notes/e2e-per-plan-map.md
git commit -m "test(e2e): add self-skipping per-plan verification specs and the plan map"
```

---

### Task 5: Interactive verification protocol (Claude-in-Chrome) and docs

**Files:**
- Create: `docs/E2E_VERIFICATION.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `.brain/gotchas.md`

**Interfaces:**
- Consumes: everything above.
- Produces: the written protocol for both verification modes — the suite (the gate) and extension-driven interactive checks (the magnifier).

- [ ] **Step 1: Write the verification doc**

Create `docs/E2E_VERIFICATION.md`:

```markdown
# E2E Verification

Two complementary modes. **The suite is the gate; the extension is the magnifier.**

## Mode 1 — the Playwright suite (the gate)

- `npm run test:e2e` in `src/Lus.UI` (needs the API on :8080, UI on :4200, `.env.e2e` filled).
- `baseline-*.spec.ts` pins today's behaviour. **Every migration plan must leave it green.**
- `ported-*.spec.ts` verifies each landed plan, and **self-skips** (404/feature-detect) until its
  feature exists — so specs merge early without sitting red.
- The plan↔spec mapping lives in `docs/superpowers/plans/notes/e2e-per-plan-map.md`. A migration
  plan is done only when its row is green.

### Conventions (ported from ArmyLuz, scars included)

- Page objects only — specs never contain raw selectors.
- `waitForLoadState('domcontentloaded')`, **never `networkidle`** — third-party iframes (Google
  sign-in, reCAPTCHA) keep the network busy forever and `networkidle` never settles.
- Assert on structure (URLs, roles, table shape, download events), not on Hebrew copy.
- Auth runs once (`fixtures/auth.setup.ts` → `e2e/.auth/user.json` storage state).
- Credentials live in `.env.e2e` (gitignored). Fixture users are local-dev only.
- Tests clean up what they create; a spec that cannot clean up is `test.fixme()` with the gap named.

## Mode 2 — interactive checks with the Claude-in-Chrome extension (the magnifier)

For exploratory verification during a port — the things a suite is bad at: visual RTL correctness,
animation smoothness, loader feel, canvas behaviour under a human eye.

Protocol for a review session:

1. Bring the stack up (`docker compose -f src/docker-compose.yml up -d`, `npm start`).
2. Ask Claude to drive the flow under test in Chrome (it uses its browser tools; it logs in with
   the `.env.e2e` dev user — never a real account).
3. The checklist for a ported feature is its plan's **exit criterion** plus its row in the e2e map.
4. Anything found becomes either a new baseline/ported spec (if automatable) or a `.brain` note
   (if inherently visual).

Findings from an interactive session are not verification. **Only a committed spec is.** The
session's job is to find what the suite should assert next.

## When each mode runs

| Moment | What runs |
|---|---|
| Before starting any migration plan | full baseline suite — must be green |
| After each plan's execution | baseline + that plan's `ported-*` spec |
| Before a Railway deploy | full suite |
| During plan review | interactive session over the plan's exit criterion |
```

- [ ] **Step 2: Update the architecture index and gotchas**

Add to the docs index in `docs/ARCHITECTURE.md`:

```markdown
- [`E2E_VERIFICATION.md`](./E2E_VERIFICATION.md) — the Playwright gate and the interactive protocol.
```

Append to `.brain/gotchas.md`:

```markdown
- **Never `waitForLoadState('networkidle')` in e2e** — Google sign-in / reCAPTCHA iframes keep the
  network busy forever. Gate on a visible element instead. (ArmyLuz scar, inherited deliberately.)
- **`ported-*.spec.ts` self-skip until their feature lands** (404/feature-detect). A skip there is
  expected mid-migration; a failure is not.
- **The e2e baseline predates the migration plans on purpose.** If a migration plan needs a
  baseline spec changed, that change is part of the plan's review, not a casual edit.
```

- [ ] **Step 3: Final full run**

```bash
cd src/Lus.UI && npm run test:e2e
```

Expected: baseline green, ported specs skipped.

- [ ] **Step 4: Commit**

```bash
cd /Users/onecity/Desktop/Yotam/Lus
git add docs/ .brain/gotchas.md
git commit -m "docs(e2e): verification protocol — suite as gate, extension as magnifier"
```

---

## Self-Review

**Coverage of the ask ("use Playwright, use the extension, check all that we move"):**

| Ask | Where |
|---|---|
| Playwright harness | Tasks 1–3, ported from ArmyLuz's proven config including its scars |
| Verify what we move | Task 4 — one self-skipping spec per migration plan + the plan↔spec map; baseline pins pre-move behaviour |
| Use the extension | Task 5 — protocol: suite is the gate, extension sessions feed new specs |

**Ordering note:** this plan should execute **before** plans 01/07/02/08 land their changes —
that is the entire value of a baseline. It has no dependency on any of them; only Task 4's two
`ported-*` specs interact with their features, and those self-skip.

**Deliberately not done:**
- CI wiring (GitHub Actions). Lus has no workflow files yet; adding CI is its own small decision
  about where the suite runs and against which database, and belongs with the deploy owner.
- Mobile/webkit projects. ArmyLuz runs device profiles; Lus's screens are desktop-first. Add a
  device project when a mobile flow exists to test.
- Visual regression (screenshot comparison). The interactive protocol covers visual checks for now;
  pixel-diff tooling is worth adding only once the doc-canvas (plan 04) exists, where it will earn
  its maintenance cost.
