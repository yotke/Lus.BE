# 2026-06-07 — .NET 9 + cookie auth + Railway/Docker

## What was done
- **Git**: initialized repo at `/Users/onecity/Desktop/Yotam/Lus` (monorepo: backend + `Lus.UI` Angular together). Author identity `yotke`. Remote NOT set yet — must point at the **yotke personal** GitHub account, *not* an org.
- **.NET 9**: `global.json` was still pinned to SDK 7 while csproj were already `net9.0`; fixed to `9.0.200` + `rollForward: latestFeature`. `dotnet build Lus.sln` → 0 errors (673 warnings: nullable + IdentityServer4/AutoMapper vuln advisories).
- **Cookie auth (front+back)**: was already scaffolded earlier in the session by another process (files written ~04:45–04:50). Verified + built on it rather than re-porting. See `/docs/AUTH_COOKIE_LOGIN.md`.
- **Railway MySQL**: `appsettings.Production.json` connection → `mysql.railway.internal:3306` / db `railway`.
- **shiftiz.com**: cookie + CSRF domain `.shiftiz.com`; CORS origins added.
- **Docker**: `src/Dockerfile`, `docker-entrypoint.sh`, `.dockerignore`, `docker-compose.yml` (mysql + api + ui).

## Open items
- `gh` not authenticated → can't create the yotke GitHub repo yet. Run `gh auth login` as yotke, then create private repo + add origin.
- MySQL password currently inline in committed `appsettings.Production.json` → move to Railway env `ConnectionStrings__DefaultConnection` before any public push.
- Tests exist but **not run** (per request).
