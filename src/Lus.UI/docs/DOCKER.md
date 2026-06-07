# Docker & Deployment

## Images

| Service | Build context | Base image | Port |
|---------|---------------|-----------|------|
| `api`   | `../` (solution root) | `mcr.microsoft.com/dotnet/aspnet:9.0` | 8080 |
| `ui`    | `./Lus.UI` | `nginx:1.27-alpine` | 80 |
| `mysql` | — | `mysql:8.0` | 3306 |

## Frontend image

`Lus.UI/Dockerfile` is a two-stage build:

1. **builder** (`node:20-alpine`) — `npm ci` + `ng build`.
2. **runtime** (`nginx:1.27-alpine`) — serves the static bundle with
   `nginx.conf` (SPA fallback, asset caching, security headers, `/health`).

```bash
docker build -t lus-ui ./Lus.UI
docker run -p 4200:80 lus-ui
```

Choose the Angular configuration at build time:

```bash
docker build --build-arg BUILD_CONFIGURATION=production -t lus-ui ./Lus.UI
```

## Full stack

From the solution root (`Lus/src`):

```bash
docker compose up --build
```

Brings up MySQL → API → UI. The API waits for MySQL's healthcheck. Defaults:

- UI:  http://localhost:4200
- API: http://localhost:8080
- DB:  localhost:3306

## shiftiz.com domain

In production the stack runs behind `shiftiz.com`:

- UI served at `https://app.shiftiz.com` (and `www.` / apex redirect).
- API at `https://api.shiftiz.com`.
- Cookies use `Domain=.shiftiz.com`, `Secure=true` so the session is shared
  across subdomains.
- CORS whitelist on the API includes the `https://*.shiftiz.com` origins.

`nginx.conf` already lists `shiftiz.com www.shiftiz.com app.shiftiz.com` in
`server_name` and serves `/.well-known/` as plain text for domain verification.

## Environment overrides

| Variable | Purpose |
|----------|---------|
| `BUILD_CONFIGURATION` | Angular build config (UI image) |
| `ConnectionStrings__DefaultConnection` | API → MySQL connection |
| `Auth__Cookie__Domain` / `Auth__Cookie__Secure` | Cookie scope |
| `Cors__Origins` | Allowed browser origins |
