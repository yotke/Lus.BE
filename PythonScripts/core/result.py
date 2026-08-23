"""The script result envelope (Phase 2; consumed per-flow behind flags).

Doctrine (plan children 02 B2 + 07 A1/A7): no silent drops — every discarded
item is reported with a STABLE machine code; correlation id + usage metrics
ride along for observability. Scripts keep emitting today's bare payload until
their C# caller opts in with --envelope (golden-pinned either way).
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

# Stable reject codes (extend here only — FE localizes off these).
RULE_ENTITY_UNRESOLVED = "RULE_ENTITY_UNRESOLVED"
UNKNOWN_TYPE = "UNKNOWN_TYPE"
MISSING_TYPE = "MISSING_TYPE"
LLM_PARSE_FAIL = "LLM_PARSE_FAIL"
LLM_UNAVAILABLE = "LLM_UNAVAILABLE"
CONNECTION_INVALID = "CONNECTION_INVALID"
INVALID_INPUT = "INVALID_INPUT"


@dataclass
class RejectedItem:
    payload: Any
    stage: str          # "deterministic" | "extract" | "normalize" | "postprocess" | ...
    code: str           # one of the stable codes above
    detail: str = ""

    def to_dict(self) -> dict:
        return {"payload": self.payload, "stage": self.stage,
                "reason": {"code": self.code, "detail": self.detail}}


@dataclass
class ScriptResult:
    ok: bool = True
    data: Any = None
    rejected: list[RejectedItem] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)
    request_id: str | None = None
    prompt_version: str | None = None
    usage: dict | None = None   # {inputTokens, outputTokens, durationMs}

    def to_dict(self) -> dict:
        out: dict = {"ok": self.ok, "data": self.data,
                     "rejected": [r.to_dict() for r in self.rejected],
                     "warnings": list(self.warnings)}
        if self.request_id:
            out["requestId"] = self.request_id
        if self.prompt_version:
            out["promptVersion"] = self.prompt_version
        if self.usage:
            out["usage"] = self.usage
        return out
