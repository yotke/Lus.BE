# P0 — Redis + SignalR + Python runtime

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (this phase is infra; no need for a subagent per file). Checkboxes (`- [ ]`) for tracking.
>
> Parent plan: [`docs/superpowers/plans/2026-08-18-ai-builders-port.md`](../docs/superpowers/plans/2026-08-18-ai-builders-port.md)
> Spec: [`docs/superpowers/specs/2026-08-18-ai-builders-port-design.md`](../docs/superpowers/specs/2026-08-18-ai-builders-port-design.md)

**Goal:** Lus Docker image can run Python (with `openpyxl`) and the API has Redis + a reachable Document Builder SignalR hub.

**Exit criterion:** `docker compose up` serves the API with a reachable hub and `python3 -c "import openpyxl"` inside the image.

**Does not include:** the kernel, `runner.py`, agents, or any builder logic. That is P1.

---

### Task 1: Redis service in docker-compose

**Files:**
- Modify: `src/docker-compose.yml`
- Modify: `docs/DEPLOYMENT_RAILWAY.md`

**Interfaces:**
- Consumes: existing `mysql` / `api` / `ui` services
- Produces: a `redis` service on 6379 that `api` depends on; env `Caching__ProviderName=redis` and `Redis__*` on `api`

- [ ] **Step 1: Add redis + wire api env**

In `src/docker-compose.yml`, after the `mysql` service and before `api`, add:

```yaml
  redis:
    image: redis:7-alpine
    restart: unless-stopped
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
```

Change the `api` service so it:

1. Builds from the **repo root** (PythonScripts live at repo root, same as Railway's root Dockerfile):

```yaml
    build:
      context: ..
      dockerfile: Dockerfile
```

2. Depends on both mysql (healthy) and redis (healthy):

```yaml
    depends_on:
      mysql:
        condition: service_healthy
      redis:
        condition: service_healthy
```

3. Adds these environment keys (keep the existing ones):

```yaml
      Caching__ProviderName: "${CACHING_PROVIDER:-redis}"
      Redis__Host: "${REDIS_HOST:-redis}"
      Redis__Port: "${REDIS_PORT:-6379}"
      Redis__Username: "${REDIS_USERNAME:-}"
      Redis__Password: "${REDIS_PASSWORD:-}"
      Redis__Ssl: "${REDIS_SSL:-false}"
      PythonSetting__PythonProviderPath: /usr/bin/python
      PythonSetting__PythonScriptFolder: /app/PythonScripts
```

Local default is Redis (the point of P0). `CACHING_PROVIDER=default` still falls back to in-memory.

- [ ] **Step 2: Document Railway Redis**

Append a Redis section to `docs/DEPLOYMENT_RAILWAY.md`:

```
## Redis (builder sessions + cache)

Add a Railway Redis plugin. Inject into the API service (never bake the password into the image):

Caching__ProviderName=redis
Redis__Host=<railway internal host>
Redis__Port=6379
Redis__Username=default
Redis__Password=<from plugin>
Redis__Ssl=false
```

- [ ] **Step 3: Commit**

```bash
git add src/docker-compose.yml docs/DEPLOYMENT_RAILWAY.md
git commit -m "$(cat <<'EOF'
infra: add Redis to local compose and document Railway wiring

EOF
)"
```

---

### Task 2: EasyCaching Redis provider

**Files:**
- Modify: `src/Lus.Api/Lus.Api.csproj`
- Modify: `src/Lus.Api/Infrastructure/Extensions/CachingExtensions.cs`
- Modify: `src/Lus.Api/appsettings.json`
- Modify: `src/Lus.Api/appsettings.Development.json`
- Modify: `src/Lus.Api/appsettings.Production.json`
- Test: `src/Lus.Api.Tests/Caching/CachingExtensionsTests.cs`

**Interfaces:**
- Consumes: `Caching:ProviderName` (`default` | `redis`); `Redis:Host/Port/Username/Password/Ssl`
- Produces: `IEasyCachingProvider` backed by Redis when `ProviderName=redis`, in-memory otherwise. Existing `IEasyCachingProvider` injections keep working.

- [ ] **Step 1: Add the package**

In `src/Lus.Api/Lus.Api.csproj`, next to `EasyCaching.InMemory`:

```xml
<PackageReference Include="EasyCaching.Redis" Version="1.9.0" />
```

Match the existing EasyCaching 1.9.0 line. Do not bump.

- [ ] **Step 2: Write the failing test**

Create `src/Lus.Api.Tests/Caching/CachingExtensionsTests.cs`:

```csharp
using EasyCaching.Core;
using FluentAssertions;
using Lus.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lus.Api.Tests.Caching;

public class CachingExtensionsTests
{
    [Fact]
    public void AddCaching_with_default_provider_resolves_in_memory()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Caching:ProviderName"] = "default"
            })
            .Build();

        services.AddCaching(config);
        using var sp = services.BuildServiceProvider();
        var cache = sp.GetRequiredService<IEasyCachingProvider>();
        cache.Should().NotBeNull();
        cache.Name.Should().Be("default");
    }

    [Fact]
    public void AddCaching_with_redis_provider_registers_without_throwing()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Caching:ProviderName"] = "redis",
                ["Redis:Host"] = "127.0.0.1",
                ["Redis:Port"] = "6379",
                ["Redis:Username"] = "",
                ["Redis:Password"] = "",
                ["Redis:Ssl"] = "false"
            })
            .Build();

        var act = () => services.AddCaching(config);
        act.Should().NotThrow();
    }
}
```

FluentAssertions is not currently in `Lus.Api.Tests.csproj`. Add:

```xml
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

- [ ] **Step 3: Run test to verify it fails**

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~CachingExtensionsTests
```

Expected: FAIL compiling `AddCaching` redis branch (not implemented) or the redis test if the method still only calls `UseInMemory`.

- [ ] **Step 4: Implement CachingExtensions**

Replace `src/Lus.Api/Infrastructure/Extensions/CachingExtensions.cs` with:

```csharp
using EasyCaching.Core.Configurations;
using Newtonsoft.Json;

namespace Lus.Infrastructure.Extensions
{
    public static class CachingExtensions
    {
        public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
        {
            var cacheProvider = configuration.GetValue<string>("Caching:ProviderName") ?? "default";

            services.AddEasyCaching(options =>
            {
                options.WithJson(
                    jsonSerializerSettingsConfigure: json => json.ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    "json");

                if (string.Equals(cacheProvider, "redis", StringComparison.OrdinalIgnoreCase))
                {
                    var host = configuration.GetValue<string>("Redis:Host") ?? "127.0.0.1";
                    var port = configuration.GetValue("Redis:Port", 6379);
                    var username = configuration.GetValue<string>("Redis:Username");
                    var password = configuration.GetValue<string>("Redis:Password");
                    var ssl = configuration.GetValue("Redis:Ssl", false);

                    options.UseRedis(config =>
                    {
                        config.DBConfig.Endpoints.Add(new ServerEndPoint(host, port));
                        if (!string.IsNullOrWhiteSpace(username))
                            config.DBConfig.Username = username;
                        if (!string.IsNullOrWhiteSpace(password))
                            config.DBConfig.Password = password;
                        config.DBConfig.IsSsl = ssl;
                    }, "redis");
                }
                else
                {
                    options.UseInMemory(cacheProvider);
                }
            });

            return services;
        }
    }
}
```

- [ ] **Step 5: Config keys**

In `src/Lus.Api/appsettings.json` under `Caching`, keep `"ProviderName": "default"` (safe for `dotnet run` without Redis). Add a sibling:

```json
"Redis": {
  "Host": "127.0.0.1",
  "Port": 6379,
  "Username": "",
  "Password": "",
  "Ssl": false
}
```

`appsettings.Development.json`: do **not** force redis (local `dotnet run` without compose should still boot). Compose overrides via env.

`appsettings.Production.json`: add `"Caching": { "ProviderName": "redis" }` and empty Redis host (Railway env fills it). Missing Redis host in prod is an ops problem, not a compile problem.

- [ ] **Step 6: Run tests**

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~CachingExtensionsTests
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/Lus.Api/Lus.Api.csproj src/Lus.Api/Infrastructure/Extensions/CachingExtensions.cs src/Lus.Api/appsettings.json src/Lus.Api/appsettings.Development.json src/Lus.Api/appsettings.Production.json src/Lus.Api.Tests/Lus.Api.Tests.csproj src/Lus.Api.Tests/Caching/CachingExtensionsTests.cs
git commit -m "$(cat <<'EOF'
feat: switch EasyCaching to Redis when Caching:ProviderName=redis

EOF
)"
```

---

### Task 3: DocumentBuilderHub stub

**Files:**
- Create: `src/Lus.Api/Infrastructure/SignalRHubs/DocumentBuilderHub.cs`
- Modify: `src/Lus.Api/Infrastructure/Extensions/EndpointsExtensions.cs`
- Test: `src/Lus.Api.Tests/SignalR/DocumentBuilderHubRouteTests.cs`

**Interfaces:**
- Consumes: existing `AddSignalR` + `MapSignalRHubs` in `Startup`
- Produces: hub at `/hub/document-builder` (WebSockets + LongPolling), cookie-auth'd like `CitiesStreetsHub`

Lus already has SignalR (`CitiesStreetsHub` at `/citiesStreetsHub`). P0 adds the builder hub path so P1/P5 have somewhere to connect. No events yet.

- [ ] **Step 1: Write the failing route test**

Create `src/Lus.Api.Tests/SignalR/DocumentBuilderHubRouteTests.cs`. The controller-route tests in this repo use TestHost; follow that. If spinning a full `Startup` is too heavy, assert via a focused mapping helper:

```csharp
using FluentAssertions;
using Lus.Infrastructure.Extensions;
using Lus.Infrastructure.SignalRHubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lus.Api.Tests.SignalR;

public class DocumentBuilderHubRouteTests
{
    [Fact]
    public void MapSignalRHubs_registers_document_builder_hub()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSignalR();
        var sp = services.BuildServiceProvider();
        var builder = new EndpointRouteBuilderStub(sp);
        builder.MapSignalRHubs();

        var endpoints = builder.DataSources.SelectMany(d => d.Endpoints).ToList();
        endpoints.Should().Contain(e =>
            e.DisplayName != null && e.DisplayName.Contains("DocumentBuilderHub"));
    }

    private sealed class EndpointRouteBuilderStub : IEndpointRouteBuilder
    {
        public EndpointRouteBuilderStub(IServiceProvider sp) => ServiceProvider = sp;
        public IServiceProvider ServiceProvider { get; }
        public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
        public IApplicationBuilder CreateApplicationBuilder() => throw new NotSupportedException();
    }
}
```

If DisplayName matching is brittle, alternatively GET negotiate:

The test host approach (preferred if existing tests already boot Startup):

Look at `src/Lus.Api.Tests/` for an existing TestHost fixture. If none, the stub above is enough.

- [ ] **Step 2: Run test — expect FAIL** (hub type missing)

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~DocumentBuilderHubRouteTests
```

- [ ] **Step 3: Create the hub**

`src/Lus.Api/Infrastructure/SignalRHubs/DocumentBuilderHub.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Lus.Authorization.Authentication;

namespace Lus.Infrastructure.SignalRHubs
{
    [Authorize(AuthenticationSchemes = CookieAuthSchemes.Api)]
    public class DocumentBuilderHub : Hub
    {
        public const string Path = "/hub/document-builder";

        public Task JoinSession(string sessionId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
    }
}
```

- [ ] **Step 4: Map it**

In `EndpointsExtensions.MapSignalRHubs`, after the cities hub:

```csharp
endpoints.MapHub<DocumentBuilderHub>(DocumentBuilderHub.Path, options =>
{
    options.Transports =
        HttpTransportType.WebSockets |
        HttpTransportType.LongPolling;
});
```

- [ ] **Step 5: Run test — expect PASS**

- [ ] **Step 6: Commit**

```bash
git add src/Lus.Api/Infrastructure/SignalRHubs/DocumentBuilderHub.cs src/Lus.Api/Infrastructure/Extensions/EndpointsExtensions.cs src/Lus.Api.Tests/SignalR/DocumentBuilderHubRouteTests.cs
git commit -m "$(cat <<'EOF'
feat: add DocumentBuilder SignalR hub at /hub/document-builder

EOF
)"
```

---

### Task 4: Python runtime in the Docker image

**Files:**
- Modify: `Dockerfile` (repo root, Railway)
- Modify: `src/Dockerfile` (keep in sync for anyone still building from `src/`)
- Create: `PythonScripts/requirements.txt`
- Create: `PythonScripts/README.md`

**Interfaces:**
- Consumes: Debian bookworm aspnet image
- Produces: `/usr/bin/python` → python3; `/app/PythonScripts` with deps including `openpyxl`

- [ ] **Step 1: Minimal PythonScripts (not the ArmyLuz org agents)**

`PythonScripts/requirements.txt`:

```
openpyxl>=3.1.0
jsonschema>=4.0.0
python-dotenv>=0.21.0
pytest>=7.0.0
```

No openai/langchain yet — those land in P4 when LLM agents exist. P0 only needs the interpreter + openpyxl.

`PythonScripts/README.md` — one paragraph: this is the Lus agent runtime; C# `PythonScriptsAdapter` (P1) runs `agents/runner.py`; do not copy ArmyLuz `agents/org`.

- [ ] **Step 2: Root Dockerfile runtime stage**

After the existing `apt-get install` of libgdiplus/ghostscript, add python3:

```dockerfile
RUN set -eux; \
    for i in 1 2 3; do apt-get update && break || (echo "apt-get update retry ($i)" && sleep 2); done; \
    apt-get install -y --no-install-recommends \
        libgdiplus \
        libc6-dev \
        ghostscript \
        fontconfig \
        libfontconfig1 \
        python3 \
        python3-pip \
    && ln -sf /usr/bin/python3 /usr/bin/python \
    && rm -rf /var/lib/apt/lists/*
```

After `COPY --from=build /publish .`:

```dockerfile
COPY PythonScripts/ /app/PythonScripts/
RUN if [ -f /app/PythonScripts/requirements.txt ]; then \
      pip install --no-cache-dir --break-system-packages -r /app/PythonScripts/requirements.txt; \
    fi

ENV PythonSetting__PythonProviderPath=/usr/bin/python
ENV PythonSetting__PythonScriptFolder=/app/PythonScripts
ENV Caching__ProviderName=default
ENV Redis__Host=""
ENV Redis__Port="6379"
ENV Redis__Username=""
ENV Redis__Password=""
ENV Redis__Ssl="false"
```

`.dockerignore` at repo root currently excludes `*.md` and `.brain` — do **not** exclude `PythonScripts`. Confirm `.dockerignore` does not ignore it.

Mirror the python install in `src/Dockerfile` so a `src/`-context build does not silently drop Python. For `src/Dockerfile`, `COPY PythonScripts/` will fail (context is `src/`). Add a comment: "prefer the root Dockerfile via compose context: ..". Still install python3 + pip in `src/Dockerfile` so an old compose file at least has the interpreter. Copying scripts is the root Dockerfile's job.

- [ ] **Step 3: Verify image (exit criterion)**

From repo root:

```bash
docker compose -f src/docker-compose.yml build api
docker compose -f src/docker-compose.yml up -d mysql redis api
docker compose -f src/docker-compose.yml exec api python3 -c "import openpyxl; print(openpyxl.__version__)"
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/health
```

Expected: openpyxl prints a 3.x version; health returns 200.

Hub reachability: `curl -i http://localhost:8080/hub/document-builder/negotiate?negotiateVersion=1` — unauthenticated may be 401, which **proves the hub is mapped**. A 404 fails the exit criterion.

- [ ] **Step 4: Commit**

```bash
git add Dockerfile src/Dockerfile PythonScripts/requirements.txt PythonScripts/README.md
git commit -m "$(cat <<'EOF'
infra: install Python and openpyxl in the API image

EOF
)"
```

---

### Task 5: Architecture index

**Files:**
- Modify: `docs/ARCHITECTURE.md`
- Modify: `.brain/README.md`

- [ ] **Step 1: Add docs index entries**

In `docs/ARCHITECTURE.md` Docs index, add:

```
- [`BUILDERS_ARCHITECTURE.md`](./BUILDERS_ARCHITECTURE.md)
- [`DOCUMENT_BUILDER.md`](./DOCUMENT_BUILDER.md)
- [`PYTHON_AGENTS_BRIDGE.md`](./PYTHON_AGENTS_BRIDGE.md)
- [`NON_BLOCKING_LOADING.md`](./NON_BLOCKING_LOADING.md)
- [`AUTH_HARDENING.md`](./AUTH_HARDENING.md)
- Spec: [`superpowers/specs/2026-08-18-ai-builders-port-design.md`](./superpowers/specs/2026-08-18-ai-builders-port-design.md)
```

Note that EasyCaching is Redis when `Caching:ProviderName=redis`, in-memory otherwise. SignalR hubs: `/citiesStreetsHub` (legacy) + `/hub/document-builder`.

- [ ] **Step 2: Update `.brain/README.md` index** with this file and P1–P8.

- [ ] **Step 3: Commit**

```bash
git add docs/ARCHITECTURE.md .brain/README.md
git commit -m "$(cat <<'EOF'
docs: index the AI builders port and P0 infra

EOF
)"
```

---

## P0 done when

- [ ] `dotnet test src/Lus.Api.Tests --filter "FullyQualifiedName~CachingExtensionsTests|FullyQualifiedName~DocumentBuilderHubRouteTests"` is green
- [ ] `docker compose -f src/docker-compose.yml exec api python3 -c "import openpyxl"` succeeds
- [ ] `GET /health` is 200
- [ ] `GET /hub/document-builder/negotiate` is not 404
