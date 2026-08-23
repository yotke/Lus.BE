# Strong-typing pass + green suite (2026-08-18)

> Session note. Owner-doc for the API-contract typing rules the port follows.
> Related: [`2026-08-18-armyluz-to-lus-port-research.md`](./2026-08-18-armyluz-to-lus-port-research.md) ·
> [`../docs/PYTHON_AGENTS_BRIDGE.md`](../docs/PYTHON_AGENTS_BRIDGE.md)

## Context

A second session had already implemented most of plan 01 and much of plan 03 (kernel, adapter,
`runner.py`, `core/`, `pyutil/`, `Documents/` entities + services, orchestrator, controller, hub).
This note records the strong-typing pass applied on top, at the user's instruction — *"we work with
strong types, front and back"* — plus the test work that made the suite green.

## Verified state after the pass

| Check | Before | After |
|---|---|---|
| `dotnet build src/Lus.sln` | 3 errors | **0 errors** |
| `dotnet test Lus.Api.Tests` | 20 passed / 2 failed | **58 passed / 0 failed** |
| `PythonScripts` pytest | 7 passed | **7 passed** |

## What was weakly typed, and what replaced it

### 1. The echo endpoint forwarded raw stdout

`DocumentBuilderController.Echo` returned `Content(raw, "application/json")` — the Python envelope
string passed straight through. No DTO, no `ProducesResponseType`, no Swagger schema, no client
type. `/turn`, `/undo`, `/redo` were already typed; `/echo` was the outlier.

**Replaced with:**

- `AgentEnvelopeDto<TResult>` (`Lus.Contracts/Common/Builders/`) — the typed face of the envelope
  contract, generic over the agent's own result shape, with `AgentErrorInfoDto` carrying `Code`
  plus the two fallback message strings.
- `EchoResultDto` — mirrors `agents/schemas/echo.result.schema.json` exactly.
- `EchoAgentInputDto` — replaces an anonymous `new { Text = … }`. The property name IS the wire
  contract the Python agent reads (`agent_input.get("Text")`), so it belongs somewhere the compiler
  checks it.
- `AgentEnvelopeParser.Parse<TResult>(raw, agent)` (`Lus.Application/Common/Builders/`) — the one
  place stdout becomes a typed envelope.
- The endpoint now returns `ActionResult<AgentEnvelopeDto<EchoResultDto>>`.

### 2. `EchoRequestDto.Text` was optional-in-practice

Declared non-nullable but defaulted to `""`, and the controller then did `body.Text ?? ""`. An
absent body reached the agent as an empty string and came back `Ok:true` — a misleading success.

**Replaced with:** `required string Text` + `[Required(AllowEmptyStrings = false)]` +
`[MaxLength(4000)]`, rejected at the model binder instead.

## The parsing law (new)

**`AgentEnvelopeParser` never throws.** A misbehaving agent must degrade one turn, not 500 the
request:

| Input | Result |
|---|---|
| valid success envelope | typed `Result` |
| valid failure envelope | `Ok:false` + `ErrorInfo` preserved |
| unreadable / empty stdout | `Ok:false`, code `envelope_unparseable` |
| `Ok:true` with null `Result` | `Ok:false`, code `empty_result` |

The last row matters: `Ok:true` with no result is a Python-side contract violation, and surfacing
it beats handing a caller a null `Result` sitting behind a true flag.

## Tests added

| File | Covers |
|---|---|
| `Builders/AgentEnvelopeParserTests.cs` | success, handled failure (both fallback strings kept), 3 unreadable-stdout shapes, 3 empty-stdout shapes, `Ok:true`-with-null-result, whitespace/newline tolerance, case-insensitive property matching |
| `Builders/EchoResultDtoMatchesPythonSchemaTests.cs` | **drift guard** — reads the real `echo.result.schema.json` at runtime: DTO property set == schema property set; every schema-`required` field is non-nullable on the DTO (via `NullabilityInfoContext`); `additionalProperties:false` is set (without it the property-set assertion is meaningless); a schema-shaped payload deserializes |

**Why the drift guard reads the file rather than duplicating the shape:** the runner validates every
agent's output against that schema *before* emitting, so the schema is the source of truth. A C#
class asserting against a copy of itself proves nothing.

## Two bugs fixed along the way

1. **`DocumentTotalsCalculatorTests.cs` contained the same namespace + class twice** (CS0101/CS0111,
   3 build errors) — a botched concurrent write. Kept the better copy (shared options field, extra
   `HasValue` guard) and deleted the duplicate.
2. **`DatabaseExtensionsTests` had been failing on `main`** — both failures pre-existing, confirmed
   by both the source and test files being byte-identical to HEAD. The tests asserted a full
   connection string and went stale when `SslMode=None;AllowPublicKeyRetrieval=true;` was added
   (Railway MySQL uses `caching_sha2_password` over a non-TLS internal link; Pomelo needs both or
   the handshake fails). Rewritten to parse the string and assert the parts, plus a third case
   pinning the local-dev fallback — so adding an option no longer breaks unrelated tests.

## The rule going forward

**No endpoint returns an untyped passthrough.** Every response has a DTO and a
`ProducesResponseType`. Every DTO crossing the Python boundary mirrors a JSON Schema, and that
mirroring is pinned by a test that reads the schema file.
