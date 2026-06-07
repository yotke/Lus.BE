# Lus.UI

Angular front-end for the **Lus / Shiftiz** platform (deployed under `shiftiz.com`).

- Angular 18 (NgModule `AppModule` bootstrap)
- Cookie-based authentication (HttpOnly session cookie + double-submit CSRF token)
- Generic, reusable table / input / button adapter library
- Server-side **search & projection** engine bindings

## Quick start

```bash
npm install
npm start            # ng serve on http://localhost:4200
```

The dev server talks to the backend defined in
`src/environments/environment.ts` (`target`).

## Build

```bash
npm run build        # production build into dist/loz-information
```

## Docker

Multi-stage Docker build (Node build stage + nginx runtime):

```bash
docker build -t lus-ui .
docker run -p 4200:80 lus-ui
```

Or run the full stack (MySQL + API + UI) from the solution root:

```bash
cd ..                # /Users/.../Lus/src
docker compose up --build
```

See `docs/DOCKER.md` for the `shiftiz.com` domain setup.

## Documentation

| Doc | Description |
|-----|-------------|
| docs/ARCHITECTURE.md  | App structure, adapters, infrastructure |
| docs/AUTH.md          | Cookie + CSRF authentication flow |
| docs/SEARCH_ENGINE.md | Search request / filtering / projection model |
| docs/DOCKER.md        | Containerization & deployment |

## Testing

```bash
npm test             # Karma + Jasmine unit tests
```
