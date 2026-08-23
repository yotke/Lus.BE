# P1 — Kernel + PythonScriptsAdapter + runner.py

> Parent: [`docs/superpowers/plans/2026-08-18-ai-builders-port.md`](../docs/superpowers/plans/2026-08-18-ai-builders-port.md)
> Spec §4.1–4.2. Docs: [`BUILDERS_ARCHITECTURE.md`](../docs/BUILDERS_ARCHITECTURE.md), [`PYTHON_AGENTS_BRIDGE.md`](../docs/PYTHON_AGENTS_BRIDGE.md)
> Source: `/Users/onecity/Desktop/projects/ArmyLuz`

**Goal:** Port the entity-agnostic kernel and the generic subprocess runner. Prove it with `doc.echo`.

**Exit criterion:** A trivial `doc.echo` agent round-trips Hebrew text end-to-end from a controller.

**Depends on:** P0 (Python in the image, hub mapped). OpenAI key is **not** required.

---

### Task 1: Port `Common/Builders` (namespace rename only)

**Files — Create (copy then rename namespaces `ArmyLuz.* → Lus.*`):**

| From ArmyLuz | To Lus |
|---|---|
| `ArmyLuz.Application/Common/Builders/IBuilderAgentCatalog.cs` | `src/Lus.Application/Common/Builders/IBuilderAgentCatalog.cs` |
| `AgentResult.cs` | same |
| `BuilderAgentClientCore.cs` | same |
| `SequentialAgentWaveRunner.cs` | same |
| `BuilderSessionStoreBase.cs` | same |
| `IBuilderEventSender.cs` | same |
| `BuilderTurnContext.cs` | same |

**Do not rewrite.** Keep `ContentAgentDescriptor.RulesEnrichment` even though Documents will not use it — it is kernel surface.

`BuilderAgentClientCore` depends on `IPythonScriptsAdapter`, `AiBuilderOptions`, `LanguageType`, `AiUserMessages`. Those are Task 2.

`BuilderSessionStoreBase` depends on `ISelfHealingStore`. Lus does not have it. **Minimal port:** a new `src/Lus.Application/Common/Services/ISelfHealingStore.cs` + in-memory/Redis implementation copied from ArmyLuz `SelfHealingStore` (search `class SelfHealingStore` there). Do not invent a different session store.

`BuilderTurnContext` uses `LanguageType`. Create `src/Lus.Contracts/Common/LanguageType.cs`:

```csharp
namespace Lus.Contracts.Common;

public enum LanguageType { He = 0, En = 1 }

public static class LanguageTypeExtensions
{
    public static string ToLangCode(this LanguageType language) =>
        language == LanguageType.He ? "he" : "en";
}
```

- [ ] Copy the 7 files, rename namespaces, fix usings.
- [ ] Add `LanguageType` + `ToLangCode`.
- [ ] Port `ISelfHealingStore` + `SelfHealingStore` (Redis/EasyCaching wrapper). ArmyLuz: search `ISelfHealingStore`.
- [ ] Port `AiBuilderOptions` to `src/Lus.Application/Common/Options/AiBuilderOptions.cs` (`AgentTimeoutSeconds`, default 60).
- [ ] Port a tiny `AiUserMessages` with TimeoutError / UnexpectedError (he + en). Do not port the whole ArmyLuz i18n catalog.
- [ ] Guard test `CommonBuildersKernel_ReferencesNoEntitySpecificTypes` (adapt the ArmyLuz test: forbidden prefixes `Lus.Application.Documents`, `Lus.Contracts.Documents`).
- [ ] `dotnet test --filter FullyQualifiedName~BuilderArchitectureGuardTests` PASS.
- [ ] Commit: `feat: port Common/Builders kernel from ArmyLuz`

---

### Task 2: `IPythonScriptsAdapter` + generic `RunAgentAsync` only

**Files:**
- Create: `src/Lus.Application/Common/Ports/IPythonScriptsAdapter.cs`
- Create: `src/Lus.Infrastructure/Adapters/PythonScriptsWS/PythonScriptsAdapter.cs`
- Create: `src/Lus.Api/Infrastructure/Extensions/PythonAdapterExtensions.cs`
- Modify: `src/Lus.Api/Startup.cs` to call `AddPythonScriptsAdapter`
- Modify: `src/Lus.Api/appsettings.json` with `PythonSetting` + `OpenAI` + `AiBuilder` (empty ApiKey allowed in Development)

**Interface:**

```csharp
public interface IPythonScriptsAdapter
{
    Task<string> RunAgentAsync(
        string agentName, string draftJson, string inputJson,
        string langCode, CancellationToken cancellationToken = default);
}

public class PythonScriptException : Exception
{
    public PythonScriptException(string userSafeMessage) : base(userSafeMessage) { }
    public PythonScriptException(string userSafeMessage, Exception inner) : base(userSafeMessage, inner) { }
}
```

Copy `RunAgentAsync` + the **static ctor** (`PYTHONUTF8=1`, `PYTHONIOENCODING=utf-8`) from ArmyLuz `PythonScriptsAdapter.cs` lines 58–204. **Do not copy** `CreateOrganizationAsync` or any other spawn site.

Constructor: `(scriptsPath, pythonExePath, apiKey, ILogger<PythonScriptsAdapter> logger)`. Drop `IServiceScopeFactory` / `IWebHostEnvironment` unless the copied method uses them (the generic runner does not — log via injected ILogger instead of Console.Error). Keep the scars:

- stdin BOM-less UTF-8
- `ThrowIfCancellationRequested` before `Process.Start`
- `Kill(entireProcessTree: true)`
- token on the stdin write

`PythonAdapterExtensions`: copy ArmyLuz file, rename namespaces. **In Development, do not throw if `OpenAI:ApiKey` is blank** (P1 is keyless). Throw in Production if you want, or leave optional until P4.

`apiKey` forwarded only when non-blank (`--api-key`).

- [ ] Unit test: `PythonScriptsAdapter_sets_PYTHONUTF8_in_static_ctor` (read env after type init).
- [ ] Commit: `feat: add PythonScriptsAdapter.RunAgentAsync subprocess bridge`

---

### Task 3: `runner.py` + `doc.echo`

**Files:**
- Create: `PythonScripts/agents/runner.py` (copy ArmyLuz, **replace registry**)
- Create: `PythonScripts/agents/doc/echo.py`
- Create: `PythonScripts/agents/doc/__init__.py`
- Create: `PythonScripts/agents/schemas/echo.result.schema.json`
- Create: `PythonScripts/pyutil/{__init__,model_router,llm_model,credits}.py` (copy)
- Create: `PythonScripts/core/{__init__,llm,env,jsonio,logging,result}.py` (copy)
- Create: `PythonScripts/tests/test_runner_aliases.py`
- Create: `PythonScripts/tests/test_echo_hebrew.py`

**runner.py registry (Lus):**

```python
AGENT_ALIASES = {
    "doc.echo": "echo",
}

def _registry():
    from agents.doc.echo import run as echo
    return {"echo": echo}
```

Keep `_emit`, `_safe_error`, `_validate_result`, stdin `utf-8-sig`, exit 0, `ensure_ascii=False`. Drop every `org.*` / `rules.*` / `generate.*` import.

**echo.py:**

```python
def run(*, draft, agent_input, lang="he"):
    text = (agent_input or {}).get("Text") or (agent_input or {}).get("text") or ""
    return {"Echo": text, "Lang": lang}
```

**schema** `echo.result.schema.json`:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["Echo", "Lang"],
  "properties": {
    "Echo": { "type": "string" },
    "Lang": { "type": "string" }
  },
  "additionalProperties": false
}
```

**test_echo_hebrew.py:** subprocess `python agents/runner.py --agent doc.echo --lang he --non-interactive --payload-stdin` with stdin `{"Draft":{},"Input":{"Text":"שלום עולם"}}`. Assert stdout JSON `Ok==true`, `Result.Echo=="שלום עולם"`, exit 0.

Also a BOM case: prepend `\ufeff` to stdin; still Ok.

Unknown agent: `--agent doc.nope` → Ok false, Code `unknown_agent`, exit 0.

- [ ] `cd PythonScripts && pytest tests/test_echo_hebrew.py tests/test_runner_aliases.py -v` PASS
- [ ] Commit: `feat: add runner.py and keyless doc.echo agent`

---

### Task 4: Echo endpoint (proves C# → Python)

**Files:**
- Create: `src/Lus.Api/Controllers/DocumentBuilderController.cs`
- Create: `src/Lus.Contracts/Documents/Builder/EchoRequestDto.cs` + `EchoResponseDto.cs`
- Test: `src/Lus.Api.Tests/Builders/DocumentBuilderEchoTests.cs`

```csharp
[ApiController]
[Route("v1/documents/builder")]
[Authorize(AuthenticationSchemes = CookieAuthSchemes.Api)]
public class DocumentBuilderController : ControllerBase
{
    [HttpPost("echo")]
    public async Task<ActionResult<EchoResponseDto>> Echo(
        [FromBody] EchoRequestDto body, CancellationToken ct)
    {
        var adapter = HttpContext.RequestServices.GetRequiredService<IPythonScriptsAdapter>();
        var raw = await adapter.RunAgentAsync(
            "doc.echo", "{}", System.Text.Json.JsonSerializer.Serialize(new { Text = body.Text }),
            "he", ct);
        return Content(raw, "application/json");
    }
}
```

Test: if Python is on PATH, integration test posts Hebrew and asserts Echo. If not (CI without Python), skip with `Skip.If`. A unit test with a fake `IPythonScriptsAdapter` always runs.

Wire `AddPythonScriptsAdapter` in `Startup.ConfigureServices`. For tests, register a fake adapter.

- [ ] Commit: `feat: echo Hebrew through DocumentBuilderController`

**P1 done when:** pytest Hebrew round-trip green; `POST v1/documents/builder/echo` with `{"text":"שלום"}` returns an Ok envelope containing `שלום`; kernel guard test green.
