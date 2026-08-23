# Python Agents Bridge

> Status: **specified 2026-08-18, not yet implemented in Lus.** Port the proven subprocess lane from ArmyLuz, not the FastAPI `ArmyLuz.Ai.PythonEngine`. Spec: [`docs/superpowers/specs/2026-08-18-ai-builders-port-design.md`](./superpowers/specs/2026-08-18-ai-builders-port-design.md).

## The only bridge

```
C#  Lus.Infrastructure/Adapters/PythonScriptsWS/PythonScriptsAdapter.RunAgentAsync
        │  pythonExePath agents/runner.py --agent doc.<name> --lang he --non-interactive --payload-stdin [--api-key K]
        │  WorkingDirectory = scriptsPath
        │  payload on STDIN: {"Draft": …, "Input": …}
        ▼
Python  PythonScripts/agents/runner.py
        → agents/doc/<agent>.py  → pyutil/model_router → OpenAI/Claude
```

There is no second transport. `IAgentTransport` is not pre-abstracted (ARCH-1). If spawn cost bites later, the adapter is one seam to swap.

## Envelope law (frozen)

One line of PascalCase JSON on stdout:

```json
{"Ok":true,"Agent":"doc.row_extractor","SchemaVersion":1,"Result":{…},"ErrorInfo":null}
{"Ok":false,"Agent":"…","SchemaVersion":1,"Result":null,
 "ErrorInfo":{"Code":"…","UserMessage":"<he>","UserMessageEn":"<en>"}}
```

- **Exit code is ALWAYS 0 for handled failures.** Non-zero means the interpreter itself died and C# raises `PythonScriptException`.
- Tracebacks to **stderr only** — never surfaced to the user.
- Every agent has `agents/schemas/<canonical>.result.schema.json`, validated by the runner **before** emitting. A violation becomes a safe `schema_validation_failed` envelope.
- Agents are **pure functions**. They never touch the DB.
- `COST:{json}` lines on stderr (`pyutil.credits.emit_cost`) for per-call metering.
- `PROGRESS:{json}` lines on stderr for narration.
- Stdout stays the pure JSON contract.

## The scars (do not "clean up")

Each is a fixed production bug. Hebrew/RTL is the happy path in this domain.

1. **Payload over stdin, not argv.** Drafts grow with the session; Windows caps a command line at ~32K.
2. **Static ctor** on `PythonScriptsAdapter` sets `PYTHONUTF8=1` + `PYTHONIOENCODING=utf-8` process-wide. Windows console defaults to cp1252 and Hebrew `json.dumps` raises `UnicodeEncodeError`.
3. **`StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)`.** A BOM makes every agent fail `invalid_input`.
4. **`ct.ThrowIfCancellationRequested()` before `Process.Start`.** On an already-cancelled token, `Register` fires synchronously, spawning and killing a process that never ran.
5. **`ct.Register(() => process.Kill(entireProcessTree: true))`.**
6. **The token is passed to the stdin write.** A killed child raises `OperationCanceledException` rather than a broken-pipe `IOException` that reads like a Python crash.
7. **Python:** `sys.stdin.reconfigure(encoding="utf-8-sig")` (tolerates a BOM), stdout reconfigured to utf-8, `ensure_ascii=False`.

## Config

```jsonc
"PythonSetting": {
  "PythonProviderPath": "/usr/bin/python",
  "PythonScriptFolder": "/app/PythonScripts"
},
"OpenAI": {
  "ApiKey": "",
  "Model": "gpt-4o-mini",
  "ModelLite": "",
  "ModelChat": "",
  "ModelContent": "",
  "ModelDeep": "",
  "ScoreContent": "7",
  "ScoreDeep": "8"
},
"AiBuilder": {
  "LlmProvider": "openai",
  "AgentTimeoutSeconds": 60
}
```

`PythonAdapterExtensions` exports non-blank model/tier values as `AIB_*` environment variables. **Never export blank** — that would shadow the Python default (parity-when-off: no tier envs ⇒ one legacy model).

Local docker-compose mounts or copies `PythonScripts/` and sets:

```
PythonSetting__PythonProviderPath=/usr/bin/python
PythonSetting__PythonScriptFolder=/app/PythonScripts
```

OpenAI key is **not** required for P0/P1 `doc.echo` (deterministic, keyless). LLM agents need it from P4.

## Model routing

`pyutil/model_router.py` is the one selection layer. Agents declare a capability tier (`lite / chat / content / deep`) centrally. A deterministic regex/counting score gates escalation. **90–95% of calls must never reach deep.** Budget-aware (`AIB_BUDGET_STATE=warning|critical`). One-tier ladder retry on empty answers.

## Layout in Lus

```
PythonScripts/
  agents/runner.py
  agents/doc/*.py
  agents/schemas/doc.*.result.schema.json
  render/{xlsx_renderer.py, pdf_renderer.py, template_reader.py}
  pyutil/{model_router.py, llm_model.py, credits.py}
  core/{llm.py, env.py, jsonio.py, logging.py, result.py}
  requirements.txt
  tests/
```

Do **not** copy ArmyLuz `agents/org/*` or `agents/rules/*`. The runner registry in Lus starts as `doc.echo` (P1) then the `doc.*` catalog (P4).

## Guard tests

- `test_runner_aliases.py` — every alias targets a registered agent; `doc.*` ⇒ module in `agents/doc/`; subprocess parity; unknown agent still yields a safe envelope with exit 0.
- `test_model_router.py` — every LLM-bearing agent in the registry has a tier row.
- Hebrew round-trip including a BOM-emitting writer.

## Typed contracts (added 2026-08-18)

**Law: no endpoint forwards raw agent stdout.** The envelope is deserialized into a contract before
it leaves the API.

| Type | Where | Role |
|---|---|---|
| `AgentEnvelopeDto<TResult>` | `Lus.Contracts/Common/Builders/` | the typed envelope; generic over the agent's result shape |
| `AgentErrorInfoDto` | same | `Code` (the localization contract) + the two fallback strings |
| `AgentEnvelopeParser.Parse<TResult>(raw, agent)` | `Lus.Application/Common/Builders/` | the single place stdout becomes a typed envelope |

An agent's C# result DTO **mirrors its JSON Schema**, and the mirroring is pinned by a test that
reads the schema file at runtime (`EchoResultDtoMatchesPythonSchemaTests`) — the runner validates
agent output against that schema before emitting, so the schema is the source of truth and a DTO
asserting against a copy of itself would prove nothing.

### The parser never throws

A misbehaving agent degrades one turn; it must not 500 the request.

| stdout | outcome |
|---|---|
| valid success envelope | typed `Result` |
| valid failure envelope | `Ok:false`, `ErrorInfo` preserved |
| unreadable or empty | `Ok:false`, code `envelope_unparseable` |
| `Ok:true` with null `Result` | `Ok:false`, code `empty_result` |

The last row is a Python-side contract violation — surfacing it beats handing a caller a null
`Result` behind a true flag.

### Adding an agent's typed surface

1. Write `agents/schemas/<agent>.result.schema.json` with `additionalProperties: false`
   (without it, a DTO property-set assertion is meaningless).
2. Add the matching `…ResultDto` with `required` members for every schema-`required` field.
3. Add an input DTO instead of an anonymous object — its property names are the wire contract the
   Python agent reads.
4. Copy `EchoResultDtoMatchesPythonSchemaTests` for the new pair.
5. Return `ActionResult<AgentEnvelopeDto<…ResultDto>>` with a `ProducesResponseType`.
