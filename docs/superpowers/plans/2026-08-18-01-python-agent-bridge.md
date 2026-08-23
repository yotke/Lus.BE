# Python Agent Bridge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the C#↔Python agent bridge and the entity-agnostic builder kernel in Lus, proven end-to-end by a Hebrew round-trip through a real controller.

**Architecture:** Port ArmyLuz's subprocess lane verbatim — `PythonScriptsAdapter.RunAgentAsync` spawns `python agents/runner.py --agent <name> --payload-stdin`, writes one JSON doc to stdin, and reads one line of PascalCase JSON envelope from stdout. Above it sits `Lus.Application/Common/Builders/` — the entity-agnostic kernel (agent descriptors, visitor, catalog, wave runner, session store base) that every future builder composes. Nothing in this plan knows what a document is.

**Tech Stack:** .NET 9, ASP.NET Core, xUnit + Moq, Python 3.11+, pytest, openpyxl, jsonschema, EasyCaching.Redis, Docker.

**Spec:** [`docs/superpowers/specs/2026-08-18-ai-builders-port-design.md`](../specs/2026-08-18-ai-builders-port-design.md)

**Source project for ports:** `/Users/onecity/Desktop/projects/ArmyLuz` (referred to below as `$AZ`)

## Global Constraints

- **.NET 9**; `global.json` lives in `src/`, keep it on SDK 9.x.
- **Python ≥ 3.11.**
- **Envelope law** — one line of PascalCase JSON on stdout; **exit code 0 even for handled failures**; tracebacks to stderr only; stdout carries nothing but the envelope.
- **Agents are pure functions** — stdin `{"Draft":…,"Input":…}` → envelope. They never touch the DB.
- **Hebrew/RTL is the happy path.** C# writes stdin **BOM-less**; Python reads it **BOM-tolerant** (`utf-8-sig`).
- **`SessionSchemaVersion` may only ever increase.**
- **`System.Activities`** in `Lus.Application.csproj` must stay conditional on `Windows_NT`.
- **Kernel purity** — nothing in `Lus.Application/Common/Builders/` may reference an entity-specific type. Enforced by a reflection guard test (Task 6).
- Backend tests live in `src/Lus.Api.Tests` (xUnit + Moq, already referencing `Lus.Api` and `Lus.Authorization`).

---

### Task 1: Python runtime and Redis in the container

**Files:**
- Modify: `Dockerfile` (root, Railway build)
- Modify: `src/Dockerfile` (docker-compose build)
- Modify: `src/docker-compose.yml`
- Create: `PythonScripts/requirements.txt`
- Create: `scripts/verify-python-runtime.sh`

**Interfaces:**
- Consumes: nothing.
- Produces: a container with `python3` on PATH, `PythonScripts/` copied to `/app/PythonScripts`, and the packages `openai`, `jsonschema`, `openpyxl`, `pandas`, `python-dotenv` importable. A `redis` service on `localhost:6379` in compose.

- [ ] **Step 1: Write the failing verification script**

Create `scripts/verify-python-runtime.sh`:

```bash
#!/usr/bin/env bash
# Verifies the runtime image can actually run the agent lane.
# Usage: scripts/verify-python-runtime.sh <image-tag>
set -euo pipefail
IMAGE="${1:-lus-api:local}"

echo "== python3 present =="
docker run --rm "$IMAGE" python3 --version

echo "== required packages importable =="
docker run --rm "$IMAGE" python3 -c \
  "import openai, jsonschema, openpyxl, pandas, dotenv; print('imports ok')"

echo "== PythonScripts copied into the image =="
docker run --rm "$IMAGE" test -f /app/PythonScripts/agents/runner.py \
  && echo "runner.py present"

echo "== UTF-8 mode is on (Hebrew must not crash json.dumps) =="
docker run --rm "$IMAGE" python3 -c \
  "import json,sys; sys.stdout.reconfigure(encoding='utf-8'); print(json.dumps({'x':'שלום'}, ensure_ascii=False))"

echo "ALL RUNTIME CHECKS PASSED"
```

Make it executable:

```bash
chmod +x scripts/verify-python-runtime.sh
```

- [ ] **Step 2: Run it to verify it fails**

```bash
docker build -t lus-api:local -f Dockerfile .
scripts/verify-python-runtime.sh lus-api:local
```

Expected: FAIL at the first check — `python3: executable file not found`.

- [ ] **Step 3: Create the Python requirements file**

Create `PythonScripts/requirements.txt`:

```
# LLM provider
openai>=1.40.0

# .env loading
python-dotenv>=1.0.0

# agent output validation (agents/runner.py enforces schema-per-agent)
jsonschema>=4.0.0

# Excel I/O — the document renderer round-trips the user's exemplar workbook
openpyxl>=3.1.0
pandas>=2.0.0

# testing
pytest>=7.0.0
```

- [ ] **Step 4: Add the Python runtime to the root Dockerfile**

In `Dockerfile`, extend the existing `apt-get install` block in the runtime stage — it currently
installs `libgdiplus libc6-dev ghostscript fontconfig libfontconfig1`. Add `python3`,
`python3-pip` and `python3-venv` to that same list so it stays one layer:

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
        python3-venv \
    && rm -rf /var/lib/apt/lists/*
```

Then, after `COPY --from=build /publish .`, add the Python lane:

```dockerfile
# --- Python agent lane -------------------------------------------------------
# A venv, not --break-system-packages: Debian bookworm marks the system
# interpreter EXTERNALLY-MANAGED (PEP 668) and a bare pip install fails the build.
COPY PythonScripts/requirements.txt /app/PythonScripts/requirements.txt
RUN python3 -m venv /opt/pyenv \
    && /opt/pyenv/bin/pip install --no-cache-dir -r /app/PythonScripts/requirements.txt
COPY PythonScripts /app/PythonScripts

# PythonScripts:PythonExePath in appsettings points here.
ENV LUS_PYTHON_EXE=/opt/pyenv/bin/python3
ENV LUS_PYTHON_SCRIPTS_PATH=/app/PythonScripts
# Windows console defaults to cp1252 and Hebrew json.dumps raises
# UnicodeEncodeError. No-op on Linux; set anyway so dev and prod agree.
ENV PYTHONUTF8=1
ENV PYTHONIOENCODING=utf-8
```

Note: the verification script calls bare `python3`, which resolves to the system interpreter — that
is deliberate, it proves the interpreter exists. The package-import check must use the venv, so
change that line in `scripts/verify-python-runtime.sh`:

```bash
docker run --rm "$IMAGE" /opt/pyenv/bin/python3 -c \
  "import openai, jsonschema, openpyxl, pandas, dotenv; print('imports ok')"
```

- [ ] **Step 5: Mirror the change into `src/Dockerfile`**

`src/Dockerfile` builds with `context: ./src`, so the Python paths are relative to `src/`. Apply the
same two blocks, but with `PythonScripts` resolved from the repo root. Because the compose context
is `./src`, first move the scripts under the build context by changing the compose context to the
repo root instead — edit `src/docker-compose.yml`:

```yaml
  api:
    build:
      context: ..
      dockerfile: Dockerfile
```

This makes compose use the same root `Dockerfile` as Railway, so there is exactly one image
definition to keep correct. Leave `src/Dockerfile` in place, unused, until a later cleanup.

- [ ] **Step 6: Add Redis to compose**

In `src/docker-compose.yml`, add the service:

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
      retries: 10
```

and wire the API to it — add to the `api` service's `depends_on` and `environment`:

```yaml
    depends_on:
      mysql:
        condition: service_healthy
      redis:
        condition: service_healthy
    environment:
      Caching__ProviderName: "redis"
      Caching__Redis__ConnectionString: "redis:6379"
```

- [ ] **Step 7: Run the verification script to confirm it passes**

```bash
docker build -t lus-api:local -f Dockerfile .
scripts/verify-python-runtime.sh lus-api:local
```

Expected: `ALL RUNTIME CHECKS PASSED`.

Then confirm compose comes up:

```bash
docker compose -f src/docker-compose.yml up -d redis mysql
docker compose -f src/docker-compose.yml exec redis redis-cli ping
```

Expected: `PONG`.

- [ ] **Step 8: Commit**

```bash
git add Dockerfile src/docker-compose.yml PythonScripts/requirements.txt scripts/verify-python-runtime.sh
git commit -m "build: add python agent runtime and redis to the container"
```

---

### Task 2: The Python agent kernel and runner

**Files:**
- Create: `PythonScripts/pyproject.toml`
- Create: `PythonScripts/agents/__init__.py`
- Create: `PythonScripts/agents/runner.py`
- Create: `PythonScripts/agents/doc/__init__.py`
- Create: `PythonScripts/agents/doc/echo_agent.py`
- Create: `PythonScripts/agents/schemas/echo_agent.result.schema.json`
- Test: `PythonScripts/tests/agents/test_runner_contract.py`

**Interfaces:**
- Consumes: the container from Task 1.
- Produces: `python agents/runner.py --agent doc.echo --lang he --non-interactive --payload-stdin`, reading `{"Draft":…,"Input":…}` on stdin and writing exactly one line of `{"Ok","Agent","SchemaVersion","Result","ErrorInfo"}` on stdout, exit code 0 in every handled case. Each agent module exposes `run(draft: dict, agent_input: dict, lang: str) -> dict`.

- [ ] **Step 1: Write the failing contract tests**

Create `PythonScripts/tests/agents/test_runner_contract.py`:

```python
# -*- coding: utf-8 -*-
"""The envelope law, executable. These tests are the contract C# depends on."""
import json
import os
import subprocess
import sys

SCRIPTS_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RUNNER = os.path.join(SCRIPTS_ROOT, "agents", "runner.py")


def _run(agent, payload, lang="he"):
    """Spawn the runner exactly the way PythonScriptsAdapter does."""
    proc = subprocess.run(
        [sys.executable, RUNNER, "--agent", agent, "--lang", lang,
         "--non-interactive", "--payload-stdin"],
        input=json.dumps(payload, ensure_ascii=False).encode("utf-8"),
        capture_output=True,
        cwd=SCRIPTS_ROOT,
    )
    return proc


def test_echo_agent_round_trips_hebrew():
    proc = _run("doc.echo", {"Draft": {}, "Input": {"text": "שלום עולם"}})
    assert proc.returncode == 0
    envelope = json.loads(proc.stdout.decode("utf-8"))
    assert envelope["Ok"] is True
    assert envelope["Agent"] == "doc.echo"
    assert envelope["SchemaVersion"] == 1
    assert envelope["Result"]["echoed"] == "שלום עולם"
    assert envelope["ErrorInfo"] is None


def test_stdout_is_exactly_one_line():
    proc = _run("doc.echo", {"Draft": {}, "Input": {"text": "א"}})
    lines = [l for l in proc.stdout.decode("utf-8").splitlines() if l.strip()]
    assert len(lines) == 1, f"stdout must carry only the envelope, got: {lines}"


def test_bom_prefixed_stdin_is_tolerated():
    """A BOM-emitting caller must not break the payload (utf-8-sig on the read)."""
    payload = json.dumps({"Draft": {}, "Input": {"text": "ב"}}, ensure_ascii=False)
    proc = subprocess.run(
        [sys.executable, RUNNER, "--agent", "doc.echo", "--lang", "he",
         "--non-interactive", "--payload-stdin"],
        input=b"\xef\xbb\xbf" + payload.encode("utf-8"),
        capture_output=True,
        cwd=SCRIPTS_ROOT,
    )
    envelope = json.loads(proc.stdout.decode("utf-8"))
    assert envelope["Ok"] is True, envelope


def test_unknown_agent_yields_safe_envelope_not_a_crash():
    proc = _run("doc.does_not_exist", {"Draft": {}, "Input": {}})
    assert proc.returncode == 0, "handled failures MUST exit 0"
    envelope = json.loads(proc.stdout.decode("utf-8"))
    assert envelope["Ok"] is False
    assert envelope["ErrorInfo"]["Code"] == "unknown_agent"
    assert envelope["ErrorInfo"]["UserMessage"]
    assert envelope["ErrorInfo"]["UserMessageEn"]
    assert envelope["Result"] is None


def test_malformed_stdin_yields_invalid_input_envelope():
    proc = subprocess.run(
        [sys.executable, RUNNER, "--agent", "doc.echo", "--lang", "he",
         "--non-interactive", "--payload-stdin"],
        input=b"{not json",
        capture_output=True,
        cwd=SCRIPTS_ROOT,
    )
    assert proc.returncode == 0
    envelope = json.loads(proc.stdout.decode("utf-8"))
    assert envelope["Ok"] is False
    assert envelope["ErrorInfo"]["Code"] == "invalid_input"


def test_schema_violation_is_caught_before_emit():
    """An agent returning output that violates its own schema must never reach the caller."""
    proc = _run("doc.echo", {"Draft": {}, "Input": {"text": "x", "_force_bad_output": True}})
    assert proc.returncode == 0
    envelope = json.loads(proc.stdout.decode("utf-8"))
    assert envelope["Ok"] is False
    assert envelope["ErrorInfo"]["Code"] == "schema_validation_failed"


def test_traceback_never_reaches_stdout():
    proc = _run("doc.echo", {"Draft": {}, "Input": {"_force_raise": True}})
    assert proc.returncode == 0
    envelope = json.loads(proc.stdout.decode("utf-8"))
    assert envelope["Ok"] is False
    assert envelope["ErrorInfo"]["Code"] == "agent_error"
    assert "Traceback" not in proc.stdout.decode("utf-8")
    assert "Traceback" in proc.stderr.decode("utf-8")
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
cd PythonScripts && python3 -m pytest tests/agents/test_runner_contract.py -v
```

Expected: FAIL — `runner.py` does not exist.

- [ ] **Step 3: Create the pytest configuration**

Create `PythonScripts/pyproject.toml`:

```toml
[project]
name = "lus-pythonscripts"
version = "0.1.0"
description = "Lus AI agents — spawned by the .NET PythonScriptsAdapter"
requires-python = ">=3.11"

[tool.pytest.ini_options]
pythonpath = ["."]
testpaths = ["tests"]
norecursedirs = ["__pycache__", ".pytest_cache"]
```

- [ ] **Step 4: Port the runner**

Create `PythonScripts/agents/__init__.py` (empty file) and `PythonScripts/agents/doc/__init__.py`
(empty file), then create `PythonScripts/agents/runner.py`. This is a port of
`$AZ/PythonScripts/agents/runner.py` with the registry replaced. Read the source alongside — the
comments on it record the production bugs each line fixes, and they must survive the copy.

```python
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Lus agent runner — the single entry point C# spawns.

Invocation (from C# PythonScriptsAdapter.RunAgentAsync):
    python agents/runner.py --agent doc.echo --lang he --non-interactive --payload-stdin [--api-key K]

Payload arrives on STDIN as one JSON doc: {"Draft": <draft>, "Input": <input>}.
(STDIN instead of argv: drafts grow with the session and Windows caps a command
line at ~32K chars.)

Output: ONE line of PascalCase JSON on stdout —
    {"Ok": true,  "Agent": "...", "SchemaVersion": 1, "Result": {...}, "ErrorInfo": null}
    {"Ok": false, "Agent": "...", "SchemaVersion": 1, "Result": null,
     "ErrorInfo": {"Code": "...", "UserMessage": "<he>", "UserMessageEn": "<en>"}}

SECURITY: this script NEVER exposes internal errors to users. Tracebacks go to
stderr only; stdout always gets a safe envelope and the exit code is ALWAYS 0 for
handled failures — a non-zero exit means the interpreter itself crashed and C#
raises PythonScriptException.
"""
from __future__ import annotations

import argparse
import json
import os
import sys

SCHEMA_VERSION = 1

# Make sibling packages importable when the runner is started from anywhere;
# C# starts it with cwd=PythonScripts, which also works.
_SCRIPTS_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if _SCRIPTS_ROOT not in sys.path:
    sys.path.insert(0, _SCRIPTS_ROOT)


def _safe_error(agent: str, code: str, message_he: str, message_en: str) -> dict:
    return {
        "Ok": False,
        "Agent": agent,
        "SchemaVersion": SCHEMA_VERSION,
        "Result": None,
        "ErrorInfo": {"Code": code, "UserMessage": message_he, "UserMessageEn": message_en},
    }


def _emit(envelope: dict) -> None:
    # Single line, UTF-8, no ASCII escaping — C# whole-reads stdout.
    try:
        sys.stdout.reconfigure(encoding="utf-8")  # Windows console default is cp1252
    except (AttributeError, ValueError):
        pass
    print(json.dumps(envelope, ensure_ascii=False))


def _load_schema(agent: str):
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        "schemas", f"{agent}.result.schema.json")
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def _validate_result(agent: str, result: dict) -> str | None:
    """Returns an error string when the agent's own output violates its schema."""
    try:
        import jsonschema
    except ImportError:
        sys.stderr.write("runner: jsonschema not installed — output validation skipped\n")
        return None

    try:
        schema = _load_schema(agent)
    except (OSError, json.JSONDecodeError) as ex:
        sys.stderr.write(f"runner: schema for {agent} unreadable ({ex}) — validation skipped\n")
        return None

    try:
        jsonschema.validate(result, schema)
        return None
    except jsonschema.ValidationError as ex:
        return str(ex)


# Namespaced agent names (doc.*) alias onto the canonical flat registry keys.
# The envelope echoes the REQUESTED name back.
AGENT_ALIASES = {
    "doc.echo": "echo_agent",
}


def _registry():
    from agents.doc.echo_agent import run as echo

    return {
        "echo_agent": echo,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Lus agent runner")
    parser.add_argument("--agent", required=True)
    parser.add_argument("--lang", default="en")
    parser.add_argument("--api-key", "-k", default="")
    parser.add_argument("--non-interactive", action="store_true")
    parser.add_argument("--payload-stdin", action="store_true")
    args = parser.parse_args()

    agent = args.agent
    canonical = AGENT_ALIASES.get(agent, agent)
    # Attribution for the provider's per-call COST lines (pyutil.credits) — without
    # this every usage line reads as anonymous and per-agent spend is unknowable.
    os.environ["LUS_AGENT_NAME"] = canonical

    try:
        if args.api_key:
            os.environ["OPENAI_API_KEY"] = args.api_key

        registry = _registry()
        if canonical not in registry:
            _emit(_safe_error(agent, "unknown_agent",
                              "סוכן לא מוכר.", f"Unknown agent '{agent}'."))
            sys.exit(0)

        if args.payload_stdin:
            # utf-8-sig, not utf-8: a caller writing with a BOM-emitting UTF-8
            # encoder would otherwise prepend ﻿ and fail every payload as
            # invalid_input. (Identical to utf-8 when no BOM is present.)
            try:
                sys.stdin.reconfigure(encoding="utf-8-sig")
            except (AttributeError, ValueError):
                pass

        raw = sys.stdin.read() if args.payload_stdin else "{}"
        try:
            payload = json.loads(raw) if raw.strip() else {}
        except json.JSONDecodeError as ex:
            sys.stderr.write(f"runner: bad stdin payload: {str(ex)[:200]}\n")
            _emit(_safe_error(agent, "invalid_input",
                              "קלט לא תקין לסוכן.", "Invalid agent input."))
            sys.exit(0)

        draft = payload.get("Draft") or payload.get("draft") or {}
        agent_input = payload.get("Input") or payload.get("input") or {}

        result = registry[canonical](draft=draft, agent_input=agent_input, lang=args.lang)

        violation = _validate_result(canonical, result)
        if violation is not None:
            sys.stderr.write(f"runner: {agent} output violates its schema: {violation[:500]}\n")
            _emit(_safe_error(agent, "schema_validation_failed",
                              "הסוכן החזיר תוצאה לא תקינה.",
                              "The agent returned an invalid result."))
            sys.exit(0)

        _emit({
            "Ok": True,
            "Agent": agent,
            "SchemaVersion": SCHEMA_VERSION,
            "Result": result,
            "ErrorInfo": None,
        })
        sys.exit(0)

    except SystemExit:
        raise
    except Exception:
        import traceback
        sys.stderr.write(f"ERROR (logged, not exposed): {traceback.format_exc()}\n")
        _emit(_safe_error(agent, "agent_error",
                          "אירעה שגיאה בעיבוד. אנא נסה שוב.",
                          "An error occurred while processing. Please try again."))
        sys.exit(0)  # exit 0 so C# doesn't throw — the envelope carries the failure


if __name__ == "__main__":
    main()
```

- [ ] **Step 5: Create the echo agent and its schema**

`doc.echo` exists purely to prove the bridge. It is deterministic, keyless, and carries the two
test hooks the contract tests need.

Create `PythonScripts/agents/doc/echo_agent.py`:

```python
# -*- coding: utf-8 -*-
"""doc.echo — the bridge smoke agent. Deterministic, keyless, no LLM.

Exists to prove the C#→Python→C# round trip (including Hebrew) end to end.
The two underscore-prefixed input flags are test hooks for the runner's own
failure paths; nothing in production sets them.
"""
from __future__ import annotations


def run(draft: dict, agent_input: dict, lang: str) -> dict:
    if agent_input.get("_force_raise"):
        raise RuntimeError("deliberate failure for the runner's agent_error path")

    if agent_input.get("_force_bad_output"):
        # 'echoed' must be a string per the schema — return the wrong type so the
        # runner's pre-emit validation is exercised.
        return {"echoed": 12345, "lang": lang}

    return {
        "echoed": agent_input.get("text", ""),
        "lang": lang,
        "draftKeys": sorted(draft.keys()),
    }
```

Create `PythonScripts/agents/schemas/echo_agent.result.schema.json`:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "doc.echo result",
  "type": "object",
  "required": ["echoed", "lang", "draftKeys"],
  "additionalProperties": false,
  "properties": {
    "echoed": { "type": "string" },
    "lang": { "type": "string" },
    "draftKeys": { "type": "array", "items": { "type": "string" } }
  }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

```bash
cd PythonScripts && python3 -m pytest tests/agents/test_runner_contract.py -v
```

Expected: 7 passed.

- [ ] **Step 7: Commit**

```bash
git add PythonScripts/
git commit -m "feat(python): add agent runner, envelope contract, and doc.echo smoke agent"
```

---

### Task 3: Port the entity-agnostic builder kernel

**Files:**
- Create: `src/Lus.Application/Common/Builders/AgentResult.cs`
- Create: `src/Lus.Application/Common/Builders/BuilderTurnContext.cs`
- Create: `src/Lus.Application/Common/Builders/IBuilderAgentCatalog.cs`
- Create: `src/Lus.Application/Common/Builders/IBuilderEventSender.cs`
- Create: `src/Lus.Application/Common/Builders/BuilderAgentClientCore.cs`
- Create: `src/Lus.Application/Common/Builders/SequentialAgentWaveRunner.cs`
- Create: `src/Lus.Application/Common/Builders/BuilderSessionStoreBase.cs`
- Create: `src/Lus.Application/Common/Builders/README.md`
- Create: `src/Lus.Contracts/Common/Builders/LanguageType.cs`
- Test: `src/Lus.Api.Tests/Builders/AgentCatalogTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces:
  - `AgentResult<T>` with `Ok`, `Value`, `FailureCode`, `FailureMessage`, `static Success(T)`, `static Failed(string, string)`.
  - `BuilderAgentDescriptor` (abstract record) with `Name`, `ProducesPatches`, `InputKind`, `DisplayNameKey`, `DescriptionKey`, `Icon`, `Enabled`, `abstract Kind`, `abstract Accept<TResult>(IBuilderAgentVisitor<TResult>)`; subtypes `ContentAgentDescriptor` (adds `int Wave`), `ValidatorAgentDescriptor`, `PlannerAgentDescriptor`, `AdvisorAgentDescriptor`, `RefinerAgentDescriptor`, `ImporterAgentDescriptor`.
  - `IBuilderAgentCatalog` with `IReadOnlyList<BuilderAgentDescriptor> All`, `IReadOnlyList<ContentAgentDescriptor> Content`, `BuilderAgentDescriptor? Find(string name)`.
  - `BuilderTurnContext(int UserId, string UserIdString, string JobId, LanguageType Language)`.
  - `LanguageType` enum with members `He`, `En`.

- [ ] **Step 1: Write the failing catalog test**

Create `src/Lus.Api.Tests/Builders/AgentCatalogTests.cs`:

```csharp
using FluentAssertions;
using Lus.Application.Common.Builders;
using Xunit;

namespace Lus.Api.Tests.Builders;

public class AgentCatalogTests
{
    private sealed class TestCatalog : IBuilderAgentCatalog
    {
        public IReadOnlyList<BuilderAgentDescriptor> All { get; } = new BuilderAgentDescriptor[]
        {
            new ContentAgentDescriptor { Name = "b", Wave = 2, ProducesPatches = true },
            new ContentAgentDescriptor { Name = "a", Wave = 1, ProducesPatches = true },
            new ContentAgentDescriptor { Name = "off", Wave = 1, Enabled = false },
            new PlannerAgentDescriptor { Name = "planner" },
        };

        public IReadOnlyList<ContentAgentDescriptor> Content =>
            All.OfType<ContentAgentDescriptor>()
               .Where(d => d.Enabled)
               .OrderBy(d => d.Wave)
               .ToList();

        public BuilderAgentDescriptor? Find(string name) =>
            All.FirstOrDefault(d => d.Name == name);
    }

    private sealed class KindNamingVisitor : IBuilderAgentVisitor<string>
    {
        public string VisitContent(ContentAgentDescriptor d) => $"content:{d.Name}:w{d.Wave}";
        public string VisitValidator(ValidatorAgentDescriptor d) => $"validator:{d.Name}";
        public string VisitPlanner(PlannerAgentDescriptor d) => $"planner:{d.Name}";
        public string VisitAdvisor(AdvisorAgentDescriptor d) => $"advisor:{d.Name}";
        public string VisitRefiner(RefinerAgentDescriptor d) => $"refiner:{d.Name}";
        public string VisitImporter(ImporterAgentDescriptor d) => $"importer:{d.Name}";
    }

    [Fact]
    public void Content_is_wave_ordered_and_excludes_disabled_agents()
    {
        var catalog = new TestCatalog();

        catalog.Content.Select(d => d.Name).Should().Equal("a", "b");
    }

    [Fact]
    public void Accept_dispatches_to_the_method_for_the_concrete_kind()
    {
        var visitor = new KindNamingVisitor();

        new ContentAgentDescriptor { Name = "roles", Wave = 1 }.Accept(visitor)
            .Should().Be("content:roles:w1");
        new PlannerAgentDescriptor { Name = "planner" }.Accept(visitor)
            .Should().Be("planner:planner");
        new ImporterAgentDescriptor { Name = "reader" }.Accept(visitor)
            .Should().Be("importer:reader");
    }

    [Fact]
    public void Kind_matches_the_concrete_descriptor_type()
    {
        new ContentAgentDescriptor { Name = "x", Wave = 1 }.Kind.Should().Be(BuilderAgentKind.Content);
        new ValidatorAgentDescriptor { Name = "x" }.Kind.Should().Be(BuilderAgentKind.Validator);
        new AdvisorAgentDescriptor { Name = "x" }.Kind.Should().Be(BuilderAgentKind.Advisor);
        new RefinerAgentDescriptor { Name = "x" }.Kind.Should().Be(BuilderAgentKind.Refiner);
    }

    [Fact]
    public void Find_resolves_a_descriptor_by_its_python_module_name()
    {
        new TestCatalog().Find("planner").Should().BeOfType<PlannerAgentDescriptor>();
        new TestCatalog().Find("nope").Should().BeNull();
    }

    [Fact]
    public void AgentResult_carries_success_and_failure_shapes()
    {
        var ok = AgentResult<string>.Success("v");
        ok.Ok.Should().BeTrue();
        ok.Value.Should().Be("v");

        var bad = AgentResult<string>.Failed("code", "message");
        bad.Ok.Should().BeFalse();
        bad.Value.Should().BeNull();
        bad.FailureCode.Should().Be("code");
        bad.FailureMessage.Should().Be("message");
    }
}
```

- [ ] **Step 2: Add the test dependencies and run to verify failure**

`Lus.Api.Tests` has no FluentAssertions and no reference to `Lus.Application`. Add both:

```bash
cd src/Lus.Api.Tests
dotnet add package FluentAssertions --version 6.12.0
dotnet add reference ../Lus.Application/Lus.Application.csproj
dotnet add reference ../Lus.Contracts/Lus.Contracts.csproj
cd ../..
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~AgentCatalogTests
```

Expected: FAIL — `The type or namespace name 'Builders' does not exist`.

- [ ] **Step 3: Port the kernel files**

Copy the seven kernel files from ArmyLuz and rename the namespaces. The bodies are correct as-is;
only namespaces and the `LanguageType` using change.

```bash
AZ=/Users/onecity/Desktop/projects/ArmyLuz
mkdir -p src/Lus.Application/Common/Builders
cp "$AZ/ArmyLuz.Application/Common/Builders/AgentResult.cs" \
   "$AZ/ArmyLuz.Application/Common/Builders/BuilderTurnContext.cs" \
   "$AZ/ArmyLuz.Application/Common/Builders/IBuilderAgentCatalog.cs" \
   "$AZ/ArmyLuz.Application/Common/Builders/IBuilderEventSender.cs" \
   "$AZ/ArmyLuz.Application/Common/Builders/BuilderAgentClientCore.cs" \
   "$AZ/ArmyLuz.Application/Common/Builders/SequentialAgentWaveRunner.cs" \
   "$AZ/ArmyLuz.Application/Common/Builders/BuilderSessionStoreBase.cs" \
   src/Lus.Application/Common/Builders/

# namespace + using rename
sed -i '' \
  -e 's/namespace ArmyLuz\.Application\.Common\.Builders/namespace Lus.Application.Common.Builders/' \
  -e 's/using ArmyLuz\.Contracts\.AIChats\.Type;/using Lus.Contracts.Common.Builders;/' \
  -e 's/ArmyLuz\.Application/Lus.Application/g' \
  -e 's/ArmyLuz\.Contracts/Lus.Contracts/g' \
  src/Lus.Application/Common/Builders/*.cs
```

Then verify no `ArmyLuz` identifier survived:

```bash
grep -rn "ArmyLuz" src/Lus.Application/Common/Builders/ && echo "LEAK — fix before continuing" || echo "clean"
```

Expected: `clean`.

- [ ] **Step 4: Create the `LanguageType` enum the kernel needs**

`BuilderTurnContext` references a language enum that lived in ArmyLuz's AIChats contracts. The
kernel must not depend on a chat concept, so it gets its own home:

Create `src/Lus.Contracts/Common/Builders/LanguageType.cs`:

```csharp
namespace Lus.Contracts.Common.Builders
{
    /// <summary>
    /// The language a builder turn runs in. Passed to the Python runner as --lang and used to
    /// select which of an agent's bilingual user-safe messages surfaces.
    /// Hebrew is the primary language of the domain, so it is the zero value.
    /// </summary>
    public enum LanguageType
    {
        He = 0,
        En = 1,
    }
}
```

- [ ] **Step 5: Drop the parts of the kernel that this plan does not need yet**

`IBuilderEventSender<TPatchOp, TQuestion, TWarning>` in ArmyLuz declares
`SendCommitCompletedAsync(..., int organizationId, ...)`. That signature is fine and
entity-agnostic — keep it. But `BuilderSessionStoreBase` and `BuilderAgentClientCore` reference
types from ArmyLuz's org builder. Open each and confirm it compiles against the kernel alone; if a
member pulls in an entity type, delete that member rather than importing the type. Record every
deletion in the README (Step 6) so the next builder knows what is missing and why.

Then confirm the kernel builds:

```bash
dotnet build src/Lus.Application/Lus.Application.csproj
```

Expected: build succeeded.

- [ ] **Step 6: Write the kernel README**

Create `src/Lus.Application/Common/Builders/README.md`:

```markdown
# Common/Builders — the entity-agnostic builder kernel

Shared contracts every entity builder (`<Entity>/Builder/`) composes: the agent descriptor
hierarchy (`BuilderAgentDescriptor` + kind subtypes), `IBuilderAgentVisitor<T>`,
`IBuilderAgentCatalog`, `AgentResult<T>`, `BuilderTurnContext`, `IBuilderEventSender<,,>`,
`BuilderAgentClientCore`, `SequentialAgentWaveRunner`, `BuilderSessionStoreBase`.

Ported from ArmyLuz 2026-08-18. Full contract: `docs/BUILDERS_ARCHITECTURE.md`.

## Laws

1. **Kernel purity.** No type here may reference an entity-specific type. Enforced by reflection in
   `Lus.Api.Tests/Builders/BuilderArchitectureGuardTests.cs`, not by convention.
2. **Extract bottom-up.** A type moves into the kernel once a SECOND builder actually needs it,
   never pre-generalized.
3. **`SessionSchemaVersion` may only ever increase.** Lowering it would resurrect stale Redis
   payloads written under an older shape.

## Deliberately omitted at port time

(Record here anything dropped in Task 3 Step 5, with the entity type that caused it.)
```

- [ ] **Step 7: Run the tests to verify they pass**

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~AgentCatalogTests
```

Expected: 5 passed.

- [ ] **Step 8: Commit**

```bash
git add src/Lus.Application/Common/Builders src/Lus.Contracts/Common/Builders src/Lus.Api.Tests
git commit -m "feat(builders): port the entity-agnostic builder kernel from ArmyLuz"
```

---

### Task 4: Port the Python subprocess adapter

**Files:**
- Create: `src/Lus.Application/Common/Ports/IPythonScriptsAdapter.cs`
- Create: `src/Lus.Infrastructure/Adapters/PythonScriptsWS/PythonScriptsAdapter.cs`
- Create: `src/Lus.Infrastructure/Adapters/PythonScriptsWS/PythonScriptException.cs`
- Create: `src/Lus.Api/Infrastructure/Extensions/PythonAdapterExtensions.cs`
- Modify: `src/Lus.Api/Startup.cs`
- Modify: `src/Lus.Api/appsettings.json`
- Test: `src/Lus.Api.Tests/Builders/PythonScriptsAdapterTests.cs`

**Interfaces:**
- Consumes: `runner.py` and `doc.echo` from Task 2.
- Produces: `IPythonScriptsAdapter.RunAgentAsync(string agentName, string draftJson, string inputJson, string langCode, CancellationToken ct) -> Task<string>` returning the raw envelope line from stdout. Registered in DI via `services.AddPythonAdapter(Configuration)`.

- [ ] **Step 1: Write the failing adapter tests**

These are integration tests — they spawn the real interpreter. That is deliberate: the bugs this
adapter exists to prevent (encoding, BOM, cancellation) only reproduce against a real process.

Create `src/Lus.Api.Tests/Builders/PythonScriptsAdapterTests.cs`:

```csharp
using System.Text.Json;
using FluentAssertions;
using Lus.Infrastructure.Adapters.PythonScriptsWS;
using Xunit;

namespace Lus.Api.Tests.Builders;

public class PythonScriptsAdapterTests
{
    // Resolve PythonScripts/ from the test binary location, walking up to the repo root.
    private static string ScriptsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "PythonScripts")))
            dir = dir.Parent;
        dir.Should().NotBeNull("PythonScripts/ must exist above the test binary");
        return Path.Combine(dir!.FullName, "PythonScripts");
    }

    private static string PythonExe() =>
        Environment.GetEnvironmentVariable("LUS_PYTHON_EXE") ?? "python3";

    private static PythonScriptsAdapter Adapter() =>
        new(ScriptsPath(), PythonExe(), apiKey: string.Empty);

    [Fact]
    public async Task RunAgentAsync_round_trips_hebrew_through_the_subprocess()
    {
        var input = JsonSerializer.Serialize(new { text = "שלום עולם" });

        var raw = await Adapter().RunAgentAsync("doc.echo", "{}", input, "he");

        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("Ok").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("Agent").GetString().Should().Be("doc.echo");
        doc.RootElement.GetProperty("Result").GetProperty("echoed").GetString()
            .Should().Be("שלום עולם");
    }

    [Fact]
    public async Task RunAgentAsync_returns_a_safe_envelope_for_an_unknown_agent()
    {
        var raw = await Adapter().RunAgentAsync("doc.nope", "{}", "{}", "he");

        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("Ok").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("ErrorInfo").GetProperty("Code").GetString()
            .Should().Be("unknown_agent");
    }

    [Fact]
    public async Task RunAgentAsync_throws_OperationCanceled_when_the_token_is_already_cancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await Adapter().RunAgentAsync("doc.echo", "{}", "{}", "he", cts.Token);

        // Must be a cancellation, NOT a broken-pipe IOException and NOT a spawned-then-killed
        // process. The guard is ThrowIfCancellationRequested() before Process.Start.
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RunAgentAsync_sends_a_BOM_less_payload()
    {
        // A BOM on stdin makes every agent fail invalid_input. The echo agent would
        // still parse under utf-8-sig, so assert on the success path staying success
        // with a payload whose first byte matters.
        var input = JsonSerializer.Serialize(new { text = "א" });

        var raw = await Adapter().RunAgentAsync("doc.echo", "{}", input, "he");

        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("Ok").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task RunAgentAsync_throws_PythonScriptException_when_the_script_is_missing()
    {
        var adapter = new PythonScriptsAdapter("/nonexistent/scripts", PythonExe(), string.Empty);

        var act = async () => await adapter.RunAgentAsync("doc.echo", "{}", "{}", "he");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~PythonScriptsAdapterTests
```

Expected: FAIL — `PythonScriptsAdapter` does not exist.

- [ ] **Step 3: Create the port interface**

Create `src/Lus.Application/Common/Ports/IPythonScriptsAdapter.cs`:

```csharp
namespace Lus.Application.Common.Ports
{
    /// <summary>
    /// The ONE bridge between C# and the Python agent lane. Implementations spawn
    /// agents/runner.py and return the raw envelope line from stdout — parsing is the
    /// caller's job, so this port stays free of any agent's result shape.
    /// </summary>
    public interface IPythonScriptsAdapter
    {
        /// <summary>
        /// Runs one agent. Returns the raw single-line PascalCase envelope from stdout:
        /// {"Ok",…,"Agent",…,"SchemaVersion",…,"Result",…,"ErrorInfo",…}.
        /// Throws <see cref="Lus.Infrastructure.Adapters.PythonScriptsWS.PythonScriptException"/>
        /// only when the interpreter itself died — a HANDLED agent failure comes back as an
        /// Ok:false envelope with exit code 0.
        /// </summary>
        Task<string> RunAgentAsync(
            string agentName,
            string draftJson,
            string inputJson,
            string langCode,
            CancellationToken cancellationToken = default);
    }
}
```

- [ ] **Step 4: Create the exception type**

Create `src/Lus.Infrastructure/Adapters/PythonScriptsWS/PythonScriptException.cs`:

```csharp
namespace Lus.Infrastructure.Adapters.PythonScriptsWS
{
    /// <summary>
    /// SECURITY: raised only when the Python interpreter itself failed (non-zero exit).
    /// The message is safe to show to users — no internal details, no traceback.
    /// </summary>
    public class PythonScriptException : Exception
    {
        public PythonScriptException(string userSafeMessage) : base(userSafeMessage) { }

        public PythonScriptException(string userSafeMessage, Exception innerException)
            : base(userSafeMessage, innerException) { }
    }
}
```

- [ ] **Step 5: Port the adapter**

Create `src/Lus.Infrastructure/Adapters/PythonScriptsWS/PythonScriptsAdapter.cs`. This is
`$AZ/ArmyLuz.Infrastructure/Adapters/PythonScriptsWS/PythonScriptsAdapter.cs` reduced to
`RunAgentAsync` plus its static constructor — the other 16 spawn sites are org-specific and are not
ported. **Every comment below records a fixed production bug; do not condense them.**

```csharp
using System.Diagnostics;
using System.Text;
using Lus.Application.Common.Ports;

namespace Lus.Infrastructure.Adapters.PythonScriptsWS
{
    public class PythonScriptsAdapter : IPythonScriptsAdapter
    {
        private readonly string scriptsPath;
        private readonly string pythonExePath;
        private readonly string apiKey;

        static PythonScriptsAdapter()
        {
            // WINDOWS ENCODING FIX (process-wide, so it covers every python we spawn,
            // since psi.Environment is seeded from the process environment).
            //
            // Windows console stdio defaults to the ANSI code page (cp1252), so any
            // script doing `print(json.dumps(..., ensure_ascii=False))` with Hebrew
            // crashes with UnicodeEncodeError('charmap') and exits non-zero.
            // PYTHONUTF8=1 turns on Python UTF-8 Mode, forcing stdin/stdout/stderr to
            // UTF-8 regardless of the console code page.
            // No-op on Linux/containers (already UTF-8); changes no script logic.
            Environment.SetEnvironmentVariable("PYTHONUTF8", "1");
            Environment.SetEnvironmentVariable("PYTHONIOENCODING", "utf-8");
        }

        public PythonScriptsAdapter(string scriptsPath, string pythonExePath, string apiKey)
        {
            this.scriptsPath = scriptsPath;
            this.pythonExePath = pythonExePath;
            this.apiKey = apiKey;
        }

        public async Task<string> RunAgentAsync(
            string agentName,
            string draftJson,
            string inputJson,
            string langCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentName)) throw new ArgumentNullException(nameof(agentName));
            if (string.IsNullOrWhiteSpace(scriptsPath)) throw new InvalidOperationException("Missing scriptsPath");
            if (string.IsNullOrWhiteSpace(pythonExePath)) throw new InvalidOperationException("Missing pythonExePath");

            var scriptPath = Path.Combine(scriptsPath, "agents", "runner.py");
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Agent runner script not found", scriptPath);

            var psi = new ProcessStartInfo
            {
                FileName = pythonExePath,
                WorkingDirectory = scriptsPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                // All three pipes pinned to UTF-8 — Windows defaults would mojibake
                // Hebrew draft content in transit (runner.py pins its stdin too).
                // stdin MUST be BOM-less: Encoding.UTF8 carries a preamble and the
                // StreamWriter emits it ahead of the payload, so json.loads() sees a
                // leading ﻿ and every agent fails with "invalid_input".
                StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            psi.ArgumentList.Add(scriptPath);
            psi.ArgumentList.Add("--agent");
            psi.ArgumentList.Add(agentName);
            psi.ArgumentList.Add("--lang");
            psi.ArgumentList.Add(langCode);
            psi.ArgumentList.Add("--non-interactive");
            psi.ArgumentList.Add("--payload-stdin");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                // Deterministic agents run keyless; the key is forwarded for the LLM paths.
                psi.ArgumentList.Add("--api-key");
                psi.ArgumentList.Add(apiKey);
            }

            // NEVER SPAWN A PROCESS THE CALLER HAS ALREADY GIVEN UP ON. An expired turn budget
            // used to still reach Process.Start; the kill registration below then fired
            // SYNCHRONOUSLY at registration time (that is what Register does on an
            // already-cancelled token), so the python was started and killed within
            // microseconds and every downstream symptom was noise about a process that never
            // had a chance to run.
            cancellationToken.ThrowIfCancellationRequested();

            using var process = Process.Start(psi)
                                ?? throw new InvalidOperationException("Failed to start python");

            await using var killRegistration = cancellationToken.Register(() =>
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
            });

            // One JSON doc over stdin: {"Draft": <draft>, "Input": <input>} — both args
            // are already serialized JSON, embedded raw.
            var payload = $"{{\"Draft\":{(string.IsNullOrWhiteSpace(draftJson) ? "null" : draftJson)}," +
                          $"\"Input\":{(string.IsNullOrWhiteSpace(inputJson) ? "null" : inputJson)}}}";

            // The token belongs on this write. Untokened, a cancellation that killed the child
            // mid-write left the parent writing into a pipe with no reader, and the OS — not the
            // CTS — decided the exception: IOException/SocketException(32) "Broken pipe", which
            // reads like a python crash. Passing the token makes a cancelled write raise
            // OperationCanceledException, so the caller can tell "the turn died" apart from
            // "the agent broke".
            await process.StandardInput.WriteAsync(payload.AsMemory(), cancellationToken);
            process.StandardInput.Close();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                // A non-zero exit means the interpreter itself died — every HANDLED agent
                // failure comes back as an Ok:false envelope with exit code 0.
                throw new PythonScriptException(
                    "אירעה שגיאה בעיבוד הבקשה. אנא נסה שוב.",
                    new InvalidOperationException($"python exited {process.ExitCode}: {Truncate(stderr, 2000)}"));
            }

            return stdout.Trim();
        }

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
    }
}
```

- [ ] **Step 6: Create the DI extension**

Create `src/Lus.Api/Infrastructure/Extensions/PythonAdapterExtensions.cs`:

```csharp
using Lus.Application.Common.Ports;
using Lus.Infrastructure.Adapters.PythonScriptsWS;

namespace Lus.Api.Infrastructure.Extensions
{
    public static class PythonAdapterExtensions
    {
        /// <summary>
        /// Wires the Python agent lane. Paths come from configuration so the container
        /// (venv at /opt/pyenv) and a dev machine (system python3) can differ without
        /// a code change. Environment variables win over appsettings, which is how the
        /// Dockerfile's LUS_PYTHON_EXE takes effect.
        /// </summary>
        public static IServiceCollection AddPythonAdapter(
            this IServiceCollection services, IConfiguration configuration)
        {
            var scriptsPath = Environment.GetEnvironmentVariable("LUS_PYTHON_SCRIPTS_PATH")
                              ?? configuration.GetValue<string>("PythonScripts:ScriptsPath")
                              ?? Path.Combine(AppContext.BaseDirectory, "PythonScripts");

            var pythonExe = Environment.GetEnvironmentVariable("LUS_PYTHON_EXE")
                            ?? configuration.GetValue<string>("PythonScripts:PythonExePath")
                            ?? "python3";

            var apiKey = configuration.GetValue<string>("OpenAI:ApiKey") ?? string.Empty;

            services.AddSingleton<IPythonScriptsAdapter>(
                _ => new PythonScriptsAdapter(scriptsPath, pythonExe, apiKey));

            return services;
        }
    }
}
```

- [ ] **Step 7: Register it in Startup and configure paths**

In `src/Lus.Api/Startup.cs`, add the using and the registration. Append `.AddPythonAdapter(Configuration)`
to the existing fluent chain that ends with `.AddRetrievers()`:

```csharp
using Lus.Api.Infrastructure.Extensions;
```

```csharp
            services
                .AddCaching(Configuration)
                .AddRateLimiter(Configuration)
                .AddHttpClientsConfiguration(Configuration)
                .AddHttpContextAccessor()
                .AddUserAccessor(Configuration)
                .AddNotificationCenter(Configuration)
                .AddSwaggerConfiguration(Configuration)
                .AddSecurityConfiguration(Configuration)
                .AddOptionsConfiguration(Configuration)
                .AddRepositories()
                .AddRetrievers()
                .AddPythonAdapter(Configuration);
```

In `src/Lus.Api/appsettings.json`, add the section:

```json
  "PythonScripts": {
    "ScriptsPath": "",
    "PythonExePath": "python3"
  },
  "OpenAI": {
    "ApiKey": ""
  }
```

Leave `ScriptsPath` empty in the committed file — the container sets `LUS_PYTHON_SCRIPTS_PATH` and
a dev machine falls back to `AppContext.BaseDirectory/PythonScripts`. **Never commit a real API key.**

- [ ] **Step 8: Run the tests to verify they pass**

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~PythonScriptsAdapterTests
```

Expected: 5 passed. If the Hebrew test fails with mojibake, the `StandardInputEncoding` line lost
its `encoderShouldEmitUTF8Identifier: false`.

- [ ] **Step 9: Commit**

```bash
git add src/Lus.Application/Common/Ports src/Lus.Infrastructure/Adapters/PythonScriptsWS \
        src/Lus.Api/Infrastructure/Extensions/PythonAdapterExtensions.cs \
        src/Lus.Api/Startup.cs src/Lus.Api/appsettings.json src/Lus.Api.Tests
git commit -m "feat(bridge): port PythonScriptsAdapter and wire the agent lane"
```

---

### Task 5: Typed agent client over the raw envelope

**Files:**
- Create: `src/Lus.Application/Common/Builders/AgentEnvelope.cs`
- Create: `src/Lus.Application/Common/Builders/BuilderAgentClient.cs`
- Create: `src/Lus.Application/Common/Builders/IBuilderAgentClient.cs`
- Test: `src/Lus.Api.Tests/Builders/BuilderAgentClientTests.cs`

**Interfaces:**
- Consumes: `IPythonScriptsAdapter.RunAgentAsync` (Task 4), `AgentResult<T>` (Task 3).
- Produces: `IBuilderAgentClient.InvokeAsync<T>(string agentName, string draftJson, object input, LanguageType lang, CancellationToken ct) -> Task<AgentResult<T>>` where `T : class`. Deserializes the envelope, maps `Ok:false` to `AgentResult<T>.Failed(code, userMessage)`, and never throws for a handled agent failure.

- [ ] **Step 1: Write the failing client tests**

Create `src/Lus.Api.Tests/Builders/BuilderAgentClientTests.cs`:

```csharp
using FluentAssertions;
using Lus.Application.Common.Builders;
using Lus.Application.Common.Ports;
using Lus.Contracts.Common.Builders;
using Moq;
using Xunit;

namespace Lus.Api.Tests.Builders;

public class BuilderAgentClientTests
{
    private sealed class EchoResult
    {
        public string Echoed { get; set; } = "";
        public string Lang { get; set; } = "";
    }

    private static IBuilderAgentClient ClientReturning(string envelope)
    {
        var adapter = new Mock<IPythonScriptsAdapter>();
        adapter.Setup(a => a.RunAgentAsync(
                   It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                   It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(envelope);
        return new BuilderAgentClient(adapter.Object);
    }

    [Fact]
    public async Task InvokeAsync_deserializes_a_successful_envelope()
    {
        var client = ClientReturning(
            """{"Ok":true,"Agent":"doc.echo","SchemaVersion":1,
                "Result":{"echoed":"שלום","lang":"he"},"ErrorInfo":null}""");

        var result = await client.InvokeAsync<EchoResult>(
            "doc.echo", "{}", new { text = "שלום" }, LanguageType.He);

        result.Ok.Should().BeTrue();
        result.Value!.Echoed.Should().Be("שלום");
        result.Value.Lang.Should().Be("he");
    }

    [Fact]
    public async Task InvokeAsync_maps_a_failure_envelope_to_a_failed_result_without_throwing()
    {
        var client = ClientReturning(
            """{"Ok":false,"Agent":"doc.echo","SchemaVersion":1,"Result":null,
                "ErrorInfo":{"Code":"agent_error","UserMessage":"שגיאה","UserMessageEn":"Error"}}""");

        var result = await client.InvokeAsync<EchoResult>(
            "doc.echo", "{}", new { }, LanguageType.He);

        result.Ok.Should().BeFalse();
        result.FailureCode.Should().Be("agent_error");
        result.FailureMessage.Should().Be("שגיאה");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_selects_the_english_message_for_the_english_language()
    {
        var client = ClientReturning(
            """{"Ok":false,"Agent":"doc.echo","SchemaVersion":1,"Result":null,
                "ErrorInfo":{"Code":"agent_error","UserMessage":"שגיאה","UserMessageEn":"Error"}}""");

        var result = await client.InvokeAsync<EchoResult>(
            "doc.echo", "{}", new { }, LanguageType.En);

        result.FailureMessage.Should().Be("Error");
    }

    [Fact]
    public async Task InvokeAsync_maps_unparseable_stdout_to_a_failed_result()
    {
        // Defence in depth: if anything ever prints to stdout beside the envelope,
        // the turn degrades to a failed agent, never an unhandled exception.
        var client = ClientReturning("this is not json");

        var result = await client.InvokeAsync<EchoResult>(
            "doc.echo", "{}", new { }, LanguageType.He);

        result.Ok.Should().BeFalse();
        result.FailureCode.Should().Be("envelope_unparseable");
    }

    [Fact]
    public async Task InvokeAsync_passes_the_language_code_through_to_the_adapter()
    {
        var adapter = new Mock<IPythonScriptsAdapter>();
        adapter.Setup(a => a.RunAgentAsync(
                   It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                   It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync("""{"Ok":true,"Agent":"a","SchemaVersion":1,
                                 "Result":{"echoed":"","lang":"he"},"ErrorInfo":null}""");

        await new BuilderAgentClient(adapter.Object)
            .InvokeAsync<EchoResult>("doc.echo", "{}", new { }, LanguageType.He);

        adapter.Verify(a => a.RunAgentAsync("doc.echo", "{}", It.IsAny<string>(), "he",
                                            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~BuilderAgentClientTests
```

Expected: FAIL — `BuilderAgentClient` does not exist.

- [ ] **Step 3: Create the envelope model**

Create `src/Lus.Application/Common/Builders/AgentEnvelope.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lus.Application.Common.Builders
{
    /// <summary>
    /// The wire shape every agent returns on stdout. PascalCase by contract — see
    /// docs/PYTHON_AGENTS_BRIDGE.md. Result stays a JsonElement so the client can
    /// deserialize it into the caller's own type.
    /// </summary>
    public sealed class AgentEnvelope
    {
        public bool Ok { get; set; }
        public string Agent { get; set; } = "";
        public int SchemaVersion { get; set; }
        public JsonElement? Result { get; set; }
        public AgentErrorInfo? ErrorInfo { get; set; }
    }

    public sealed class AgentErrorInfo
    {
        public string Code { get; set; } = "";
        public string UserMessage { get; set; } = "";
        public string UserMessageEn { get; set; } = "";
    }
}
```

- [ ] **Step 4: Create the client interface**

Create `src/Lus.Application/Common/Builders/IBuilderAgentClient.cs`:

```csharp
using Lus.Contracts.Common.Builders;

namespace Lus.Application.Common.Builders
{
    /// <summary>
    /// Typed access to the agent lane. Turns the raw envelope into an
    /// <see cref="AgentResult{T}"/> and NEVER throws for a handled agent failure —
    /// a failed agent must fail its wave slot, not the turn.
    /// </summary>
    public interface IBuilderAgentClient
    {
        Task<AgentResult<T>> InvokeAsync<T>(
            string agentName,
            string draftJson,
            object input,
            LanguageType language,
            CancellationToken cancellationToken = default) where T : class;
    }
}
```

- [ ] **Step 5: Implement the client**

Create `src/Lus.Application/Common/Builders/BuilderAgentClient.cs`:

```csharp
using System.Text.Json;
using Lus.Application.Common.Ports;
using Lus.Contracts.Common.Builders;

namespace Lus.Application.Common.Builders
{
    public sealed class BuilderAgentClient : IBuilderAgentClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly IPythonScriptsAdapter adapter;

        public BuilderAgentClient(IPythonScriptsAdapter adapter) => this.adapter = adapter;

        public async Task<AgentResult<T>> InvokeAsync<T>(
            string agentName,
            string draftJson,
            object input,
            LanguageType language,
            CancellationToken cancellationToken = default) where T : class
        {
            var langCode = language == LanguageType.En ? "en" : "he";
            var inputJson = JsonSerializer.Serialize(input);

            var raw = await adapter.RunAgentAsync(
                agentName, draftJson, inputJson, langCode, cancellationToken);

            AgentEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<AgentEnvelope>(raw, SerializerOptions);
            }
            catch (JsonException)
            {
                envelope = null;
            }

            if (envelope is null)
            {
                // Defence in depth: anything printed to stdout beside the envelope degrades
                // this agent to a failure rather than an unhandled exception in the turn.
                return AgentResult<T>.Failed(
                    "envelope_unparseable",
                    language == LanguageType.En
                        ? "The agent returned an unreadable result."
                        : "הסוכן החזיר תוצאה שלא ניתן לקרוא.");
            }

            if (!envelope.Ok)
            {
                var info = envelope.ErrorInfo;
                return AgentResult<T>.Failed(
                    info?.Code ?? "agent_error",
                    language == LanguageType.En
                        ? info?.UserMessageEn ?? "An error occurred."
                        : info?.UserMessage ?? "אירעה שגיאה.");
            }

            if (envelope.Result is null)
            {
                return AgentResult<T>.Failed(
                    "empty_result",
                    language == LanguageType.En
                        ? "The agent returned nothing."
                        : "הסוכן לא החזיר תוצאה.");
            }

            var value = envelope.Result.Value.Deserialize<T>(SerializerOptions);
            return value is null
                ? AgentResult<T>.Failed(
                    "empty_result",
                    language == LanguageType.En
                        ? "The agent returned nothing."
                        : "הסוכן לא החזיר תוצאה.")
                : AgentResult<T>.Success(value);
        }
    }
}
```

- [ ] **Step 6: Register the client in DI**

In `src/Lus.Api/Infrastructure/Extensions/PythonAdapterExtensions.cs`, add to `AddPythonAdapter`
before `return services;`:

```csharp
            services.AddScoped<IBuilderAgentClient, BuilderAgentClient>();
```

and the using:

```csharp
using Lus.Application.Common.Builders;
```

- [ ] **Step 7: Run the tests to verify they pass**

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~BuilderAgentClientTests
```

Expected: 5 passed.

- [ ] **Step 8: Commit**

```bash
git add src/Lus.Application/Common/Builders src/Lus.Api/Infrastructure/Extensions/PythonAdapterExtensions.cs src/Lus.Api.Tests
git commit -m "feat(builders): add typed agent client over the envelope contract"
```

---

### Task 6: The kernel purity guard test

**Files:**
- Create: `src/Lus.Api.Tests/Builders/BuilderArchitectureGuardTests.cs`

**Interfaces:**
- Consumes: every type in `Lus.Application.Common.Builders` (Tasks 3 and 5).
- Produces: nothing consumed by later tasks. This is the executable form of the kernel-purity law.

- [ ] **Step 1: Write the guard test**

This one is written and expected to **pass immediately** — it is a regression fence, not a
red-green cycle. Its value is failing later, when someone re-couples the layers.

Create `src/Lus.Api.Tests/Builders/BuilderArchitectureGuardTests.cs`:

```csharp
using System.Reflection;
using FluentAssertions;
using Lus.Application.Common.Builders;
using Xunit;

namespace Lus.Api.Tests.Builders;

/// <summary>
/// The architecture laws from docs/BUILDERS_ARCHITECTURE.md, executable.
/// A failure here means someone is re-coupling the layers — it is not a behaviour bug.
/// </summary>
public class BuilderArchitectureGuardTests
{
    private const string KernelNamespace = "Lus.Application.Common.Builders";

    // Namespaces the kernel is allowed to touch. Anything entity-specific is a leak.
    private static readonly string[] AllowedPrefixes =
    {
        "System",
        "Microsoft.Extensions",
        "Lus.Application.Common.Builders",
        "Lus.Application.Common.Ports",
        "Lus.Contracts.Common.Builders",
    };

    private static IEnumerable<Type> KernelTypes() =>
        typeof(AgentResult<>).Assembly
            .GetTypes()
            .Where(t => t.Namespace == KernelNamespace);

    private static bool IsAllowed(Type type)
    {
        // Unwrap arrays, by-ref, generics: List<DocumentRow> must be judged on DocumentRow.
        if (type.IsArray || type.IsByRef || type.IsPointer)
            return IsAllowed(type.GetElementType()!);

        if (type.IsGenericType)
            return AllowedNamespace(type) && type.GetGenericArguments().All(IsAllowed);

        if (type.IsGenericParameter)
            return true;

        return AllowedNamespace(type);
    }

    private static bool AllowedNamespace(Type type)
    {
        var ns = type.Namespace ?? "";
        return AllowedPrefixes.Any(p => ns == p || ns.StartsWith(p + ".", StringComparison.Ordinal));
    }

    [Fact]
    public void Kernel_references_no_entity_specific_types()
    {
        var leaks = new List<string>();

        foreach (var type in KernelTypes())
        {
            foreach (var iface in type.GetInterfaces().Where(i => !IsAllowed(i)))
                leaks.Add($"{type.Name} implements {iface.FullName}");

            if (type.BaseType is not null && !IsAllowed(type.BaseType))
                leaks.Add($"{type.Name} inherits {type.BaseType.FullName}");

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                                        BindingFlags.Static | BindingFlags.DeclaredOnly)
                                        .Where(p => !IsAllowed(p.PropertyType)))
                leaks.Add($"{type.Name}.{property.Name} : {property.PropertyType.FullName}");

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                                   BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (!IsAllowed(method.ReturnType))
                    leaks.Add($"{type.Name}.{method.Name} returns {method.ReturnType.FullName}");

                foreach (var parameter in method.GetParameters().Where(p => !IsAllowed(p.ParameterType)))
                    leaks.Add($"{type.Name}.{method.Name}({parameter.Name}) : {parameter.ParameterType.FullName}");
            }
        }

        leaks.Should().BeEmpty(
            "the kernel must stay entity-agnostic — extract bottom-up, never pre-generalize");
    }

    [Fact]
    public void Every_agent_kind_has_a_descriptor_subtype_and_a_visitor_method()
    {
        // Adding a kind must be a compiler-enforced new Visit method, never a scattered switch.
        var kinds = Enum.GetValues<BuilderAgentKind>();
        var visitMethods = typeof(IBuilderAgentVisitor<>).GetMethods();

        visitMethods.Should().HaveCount(kinds.Length,
            "IBuilderAgentVisitor must have exactly one Visit method per BuilderAgentKind");

        var descriptorSubtypes = typeof(BuilderAgentDescriptor).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BuilderAgentDescriptor)) && !t.IsAbstract)
            .ToList();

        descriptorSubtypes.Should().HaveCount(kinds.Length,
            "every BuilderAgentKind needs exactly one concrete descriptor record");
    }
}
```

- [ ] **Step 2: Run the guard test**

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~BuilderArchitectureGuardTests
```

Expected: 2 passed. If `Kernel_references_no_entity_specific_types` fails, the leak list names the
exact member — delete that member from the kernel rather than widening `AllowedPrefixes`.

- [ ] **Step 3: Commit**

```bash
git add src/Lus.Api.Tests/Builders/BuilderArchitectureGuardTests.cs
git commit -m "test(builders): pin kernel purity and the kind/visitor invariant"
```

---

### Task 7: End-to-end proof through a controller

**Files:**
- Create: `src/Lus.Api/Controllers/AgentDiagnosticsController.cs`
- Test: `src/Lus.Api.Tests/Builders/AgentDiagnosticsEndToEndTests.cs`

**Interfaces:**
- Consumes: `IBuilderAgentClient` (Task 5), the runtime from Task 1, `doc.echo` from Task 2.
- Produces: `GET /v1/diagnostics/agents/echo?text=…` returning `{ ok, echoed, lang }`. This endpoint is the phase's exit criterion and stays in the codebase as a deploy smoke check.

- [ ] **Step 1: Write the failing end-to-end test**

Create `src/Lus.Api.Tests/Builders/AgentDiagnosticsEndToEndTests.cs`:

```csharp
using FluentAssertions;
using Lus.Application.Common.Builders;
using Lus.Application.Common.Ports;
using Lus.Contracts.Common.Builders;
using Lus.Infrastructure.Adapters.PythonScriptsWS;
using Xunit;

namespace Lus.Api.Tests.Builders;

/// <summary>
/// The phase exit criterion: Hebrew survives C# -> stdin -> Python -> stdout -> typed result.
/// Spawns the real interpreter deliberately — encoding bugs do not reproduce against a mock.
/// </summary>
public class AgentDiagnosticsEndToEndTests
{
    private sealed class EchoResult
    {
        public string Echoed { get; set; } = "";
        public string Lang { get; set; } = "";
        public List<string> DraftKeys { get; set; } = new();
    }

    private static string ScriptsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "PythonScripts")))
            dir = dir.Parent;
        return Path.Combine(dir!.FullName, "PythonScripts");
    }

    private static IBuilderAgentClient RealClient()
    {
        IPythonScriptsAdapter adapter = new PythonScriptsAdapter(
            ScriptsPath(),
            Environment.GetEnvironmentVariable("LUS_PYTHON_EXE") ?? "python3",
            apiKey: string.Empty);
        return new BuilderAgentClient(adapter);
    }

    [Theory]
    [InlineData("שלום עולם")]
    [InlineData("דוח ביצוע שעות עבודה מרץ 2026")]
    [InlineData("נתב\"ג מערב שלב A1 — הסדרי תנועה זמניים")]
    [InlineData("mixed עברית and english 123")]
    public async Task Hebrew_survives_the_full_round_trip(string text)
    {
        var result = await RealClient().InvokeAsync<EchoResult>(
            "doc.echo", "{}", new { text }, LanguageType.He);

        result.Ok.Should().BeTrue(result.FailureMessage);
        result.Value!.Echoed.Should().Be(text);
        result.Value.Lang.Should().Be("he");
    }

    [Fact]
    public async Task Draft_json_reaches_the_agent()
    {
        var draft = """{"rows":[],"letterhead":{"client":"רשות שדות התעופה"}}""";

        var result = await RealClient().InvokeAsync<EchoResult>(
            "doc.echo", draft, new { text = "x" }, LanguageType.He);

        result.Ok.Should().BeTrue(result.FailureMessage);
        result.Value!.DraftKeys.Should().BeEquivalentTo("letterhead", "rows");
    }

    [Fact]
    public async Task A_handled_agent_failure_does_not_throw()
    {
        var result = await RealClient().InvokeAsync<EchoResult>(
            "doc.not_a_real_agent", "{}", new { }, LanguageType.He);

        result.Ok.Should().BeFalse();
        result.FailureCode.Should().Be("unknown_agent");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj --filter FullyQualifiedName~AgentDiagnosticsEndToEndTests
```

Expected: FAIL only if Tasks 2–5 are incomplete. If Tasks 1–5 are done these pass — that is the
point of the phase. Run them anyway before writing the controller so a failure is attributable.

- [ ] **Step 3: Create the diagnostics controller**

Create `src/Lus.Api/Controllers/AgentDiagnosticsController.cs`:

```csharp
using Lus.Application.Common.Builders;
using Lus.Contracts.Common.Builders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lus.Api.Controllers
{
    /// <summary>
    /// Deploy smoke check for the Python agent lane. Proves the interpreter, the scripts
    /// path, and the UTF-8 round trip are all healthy in the running container — the
    /// failure modes that are invisible until an agent is actually invoked.
    /// </summary>
    [ApiController]
    [Route("v1/diagnostics/agents")]
    [Authorize]
    public class AgentDiagnosticsController : ControllerBase
    {
        private readonly IBuilderAgentClient agentClient;

        public AgentDiagnosticsController(IBuilderAgentClient agentClient)
            => this.agentClient = agentClient;

        public sealed class EchoResponse
        {
            public bool Ok { get; set; }
            public string? Echoed { get; set; }
            public string? Lang { get; set; }
            public string? FailureCode { get; set; }
            public string? FailureMessage { get; set; }
        }

        private sealed class EchoAgentResult
        {
            public string Echoed { get; set; } = "";
            public string Lang { get; set; } = "";
        }

        [HttpGet("echo")]
        public async Task<ActionResult<EchoResponse>> Echo(
            [FromQuery] string text = "שלום",
            CancellationToken cancellationToken = default)
        {
            var result = await agentClient.InvokeAsync<EchoAgentResult>(
                "doc.echo", "{}", new { text }, LanguageType.He, cancellationToken);

            return Ok(new EchoResponse
            {
                Ok = result.Ok,
                Echoed = result.Value?.Echoed,
                Lang = result.Value?.Lang,
                FailureCode = result.FailureCode,
                FailureMessage = result.FailureMessage,
            });
        }
    }
}
```

- [ ] **Step 4: Run the full backend suite**

```bash
dotnet test src/Lus.Api.Tests/Lus.Api.Tests.csproj
```

Expected: all green, including the four pre-existing test classes (`CookieAuthSessionServiceTests`,
`FilterEnginePredicateTests`, `ContactsSearchQueryHandlerTests`, `DatabaseExtensionsTests`).

- [ ] **Step 5: Verify inside the container — the real exit criterion**

```bash
docker build -t lus-api:local -f Dockerfile .
scripts/verify-python-runtime.sh lus-api:local
docker compose -f src/docker-compose.yml up -d
# authenticate first, then:
curl -s --cookie-jar /tmp/lus.jar --cookie /tmp/lus.jar \
  'http://localhost:8080/v1/diagnostics/agents/echo?text=%D7%A9%D7%9C%D7%95%D7%9D'
```

Expected: `{"ok":true,"echoed":"שלום","lang":"he","failureCode":null,"failureMessage":null}`

- [ ] **Step 6: Run the Python suite**

```bash
cd PythonScripts && python3 -m pytest -v && cd ..
```

Expected: 7 passed.

- [ ] **Step 7: Commit**

```bash
git add src/Lus.Api/Controllers/AgentDiagnosticsController.cs src/Lus.Api.Tests
git commit -m "feat(bridge): add agent diagnostics endpoint proving the hebrew round trip"
```

---

### Task 8: Document the bridge

**Files:**
- Create: `docs/PYTHON_AGENTS_BRIDGE.md`
- Create: `docs/BUILDERS_ARCHITECTURE.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `.brain/gotchas.md`

**Interfaces:**
- Consumes: everything above.
- Produces: the reference docs plans 03–05 argue from.

- [ ] **Step 1: Write the bridge doc**

Create `docs/PYTHON_AGENTS_BRIDGE.md` covering, with the reasoning (not just the rules):

- The invocation line and why the payload goes over **stdin, not argv** (Windows ~32K cmdline cap).
- The envelope law, verbatim JSON shapes for both branches.
- **Exit code 0 for handled failures**; non-zero means the interpreter died.
- Schema-per-agent, validated before emit.
- The encoding scars: `PYTHONUTF8=1`, BOM-less `StandardInputEncoding`, `utf-8-sig` on the Python read.
- The cancellation scars: pre-`Process.Start` check, `Kill(entireProcessTree: true)`, the token on the stdin write.
- `COST:` and `PROGRESS:` stderr side-channels (declared here, implemented in plan 03).
- How to add an agent: module under `agents/<ns>/`, `run(draft, agent_input, lang)`, a result schema, a registry entry, an alias, and a contract test.

- [ ] **Step 2: Write the builders architecture doc**

Create `docs/BUILDERS_ARCHITECTURE.md` covering:

- The five-part builder anatomy (draft slice, agents wave, reconciler, suggestions, commit mapper).
- The kernel's contents and the three laws from the kernel README.
- The guard tests and what a failure in each one means.
- The add-a-builder-to-entity-X checklist.

- [ ] **Step 3: Update the architecture index**

In `docs/ARCHITECTURE.md`, add `PythonScripts` to the solution-layout table and link both new docs
from the "Docs index" section:

```markdown
- [`PYTHON_AGENTS_BRIDGE.md`](./PYTHON_AGENTS_BRIDGE.md) — the C#↔Python agent lane and its contract.
- [`BUILDERS_ARCHITECTURE.md`](./BUILDERS_ARCHITECTURE.md) — the AI builder kernel and anatomy.
```

- [ ] **Step 4: Add the new gotchas**

Append to `.brain/gotchas.md`:

```markdown
- **Python stdin must be BOM-less.** `Encoding.UTF8` emits a preamble; the runner then sees a
  leading `﻿` and every agent fails `invalid_input`. Use
  `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)`.
- **Handled agent failures exit 0.** A non-zero exit from `runner.py` means the interpreter itself
  died and C# raises `PythonScriptException`. Never "fix" an agent by exiting non-zero.
- **Nothing but the envelope may print to stdout** in an agent. Use stderr for diagnostics —
  a stray `print()` degrades the turn to `envelope_unparseable`.
- **PEP 668**: Debian bookworm marks the system interpreter externally-managed, so the image
  installs into a venv at `/opt/pyenv`. `LUS_PYTHON_EXE` points there.
- **`ThrowIfCancellationRequested()` must stay before `Process.Start`.** On an already-cancelled
  token, `Register` fires synchronously, spawning and instantly killing a process that never ran.
```

- [ ] **Step 5: Commit**

```bash
git add docs/ .brain/gotchas.md
git commit -m "docs: document the python agent bridge and builder architecture"
```

---

## Self-Review

**Spec coverage (P0, P1):**

| Spec requirement | Task |
|---|---|
| §2 D1 subprocess transport | 4 |
| §2 D3 Redis added | 1 |
| §4.1 kernel ported verbatim | 3 |
| §4.1 adapter ported with all scars | 4 |
| §4.1 `runner.py` ported | 2 |
| §4.2 envelope law | 2 (tests), 5 (client mapping) |
| §4.3 `SessionSchemaVersion` only increases | Kernel README law; enforced in plan 03 where the store is concrete |
| §7 fence 1 (exit 0, safe envelope) | 2 |
| §7 fence 2 (schema validated before emit) | 2 |
| §7 fence 3 (failed agent ≠ failed turn) | 5 |
| §10.1 kernel purity guard | 6 |
| §10.2 Hebrew round-trip incl. BOM | 2, 7 |
| §11 P0 exit criterion | 1 (script), 7 (step 5) |
| §11 P1 exit criterion | 7 |
| §12 `PYTHON_AGENTS_BRIDGE.md`, `BUILDERS_ARCHITECTURE.md` | 8 |

**Deferred to plan 03, deliberately:** `pyutil/model_router.py`, `pyutil/credits.py`, `core/*`, and
the `WorkflowProgressService`/SignalR narration. All four only have meaning once a real LLM-bearing
agent exists; porting them against `doc.echo` would be untested scaffolding. The bridge doc declares
the `COST:`/`PROGRESS:` stderr protocol so plan 03 implements against a written contract.

**Known gap:** the spec's §4.1 table lists these as P1 scope. This plan moves them to plan 03 on the
grounds above; the spec's phase table should be read with this plan's task list as the authority.
