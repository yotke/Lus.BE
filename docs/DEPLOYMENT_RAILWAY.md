# Deployment — Docker + Railway (shiftiz.com)

## Docker (in `src/`)

- **`Dockerfile`** — multi-stage: SDK 9 build → `dotnet publish` → ASP.NET 9 runtime. Installs native libs (`libgdiplus`, `ghostscript`, `fontconfig`) needed by the PDF stack (`itext7`, `GhostScript.NetCore`, `Select.HtmlToPdf.NetCore`). Railway-aware: binds Kestrel to `$PORT` via `docker-entrypoint.sh`.
- **`docker-entrypoint.sh`** — `exec dotnet Lus.Api.dll --urls http://+:${PORT:-8080}`.
- **`.dockerignore`** — excludes `bin/obj/publish`, `node_modules`, `Lus.UI`, etc.
- **`docker-compose.yml`** — local stack: `mysql` (8.0) + `api` + `ui`.

> Note: `Lus.Application.csproj` has a legacy, unused `System.Activities` reference made **Windows-only** (`Condition="'$(OS)' == 'Windows_NT'"`) so Linux/Docker builds succeed.

## Railway

The API runs inside Railway's private network and talks to the managed MySQL over the **internal** host.

### MySQL service variables (from Railway)
| Var | Value |
|---|---|
| `MYSQLHOST` | `mysql.railway.internal` |
| `MYSQLPORT` | `3306` |
| `MYSQLDATABASE` / `MYSQL_DATABASE` | `railway` |
| `MYSQLUSER` | `root` |
| `MYSQL_URL` (internal) | `mysql://root:***@mysql.railway.internal:3306/railway` |
| `MYSQL_PUBLIC_URL` (external) | `mysql://root:***@shortline.proxy.rlwy.net:58585/railway` |

### Connection string
`appsettings.Production.json` → `ConnectionStrings:DefaultConnection`:
```
Server=mysql.railway.internal;Port=3306;Database=railway;User=root;Password=***;SslMode=None;AllowPublicKeyRetrieval=true;
```

**Security:** prefer setting the Railway env var `ConnectionStrings__DefaultConnection` (double underscore) so the password is **not** committed. .NET configuration env vars override appsettings automatically. Remove the inline password from the committed file before any public push.

## Domain — shiftiz.com

- Cookie + CSRF `Domain` = `.shiftiz.com` (leading dot → shared across subdomains: `app.`, `api.`).
- CORS origins include `https://shiftiz.com`, `https://www.shiftiz.com`, `https://app.shiftiz.com`, `https://api.shiftiz.com`.
- `Auth:Cookie:Secure = true` in production (HTTPS required for `Secure` cookies).

## Env-var overrides (Railway service variables)
```
ASPNETCORE_ENVIRONMENT=Production
PORT=8080
ConnectionStrings__DefaultConnection=Server=mysql.railway.internal;Port=3306;Database=railway;User=root;Password=...;SslMode=None;AllowPublicKeyRetrieval=true;
Auth__Cookie__Domain=.shiftiz.com
Cors__Origins=https://shiftiz.com,https://www.shiftiz.com,https://app.shiftiz.com,https://api.shiftiz.com
```
