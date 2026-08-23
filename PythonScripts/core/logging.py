"""The stderr wire protocol the C# adapter parses (Phase 2).

Codifies the existing conventions (PythonScriptsAdapter.cs:276-380 parses
PROGRESS:/ENTITY_CREATED:/ENTITIES_CREATED:; MeteredPythonExecutor parses
COST:). stdout stays JSON-only — everything human/machine-progress goes here.
"""
from __future__ import annotations

import json
import sys
from typing import Any


def debug(msg: str) -> None:
    print(f"DEBUG {msg}", file=sys.stderr)


def progress(stage: str, percent: int | None = None, *, title: str = "",
             message: str = "", description: str = "", icon: str = "",
             details: Any = None) -> None:
    payload: dict = {"stage": stage}
    if percent is not None:
        payload["percent"] = percent
    for k, v in (("title", title), ("message", message),
                 ("description", description), ("icon", icon)):
        if v:
            payload[k] = v
    if details is not None:
        payload["details"] = details
    print("PROGRESS:" + json.dumps(payload, ensure_ascii=False), file=sys.stderr, flush=True)


def entity_created(entity: dict) -> None:
    print("ENTITY_CREATED:" + json.dumps(entity, ensure_ascii=False), file=sys.stderr, flush=True)


def cost(payload: dict) -> None:
    print("COST:" + json.dumps(payload, ensure_ascii=False), file=sys.stderr, flush=True)
