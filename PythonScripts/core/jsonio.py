"""Tolerant JSON extraction + canonical stdout emission (Phase 2).

`extract_json_objects` is THE single implementation of "parse whatever the
LLM returned". The algorithm (and its stderr DEBUG trace) is moved VERBATIM
from llm_rules/extract.py so delegation is behavior-identical — handles
```json fences, JSON arrays, JSONL, glued objects, and brace scanning.
"""
from __future__ import annotations

import json
import re
import sys
from typing import Any, Dict, List


def split_top_level_json_objects(s: str) -> List[str]:
    """Split concatenated top-level {...}{...} objects (string-safe)."""
    objs: List[str] = []
    depth = 0
    start = None
    in_str = False
    esc = False
    for i, ch in enumerate(s):
        if esc:
            esc = False
            continue
        if ch == "\\":
            esc = True
            continue
        if ch == '"':
            in_str = not in_str
            continue
        if in_str:
            continue
        if ch == "{":
            if depth == 0:
                start = i
            depth += 1
            continue
        if ch == "}":
            depth -= 1
            if depth == 0 and start is not None:
                objs.append(s[start:i + 1])
                start = None
            continue
    return objs


def extract_json_objects(raw: str) -> List[Dict[str, Any]]:
    """
    Accepts any LLM output and returns a list of dicts.
    Handles ```json fences, JSON arrays, JSONL, and concatenated objects.
    """
    text = (raw or "").strip()

    print(f"DEBUG _extract_json_objects input length: {len(text)}", file=sys.stderr)

    # 1) pull out fenced blocks, else use whole text
    fences = re.findall(r"```(?:json)?\s*([\s\S]*?)```", text, flags=re.IGNORECASE)
    sections = fences if fences else [text]

    objs: List[Dict[str, Any]] = []

    for sec in sections:
        sec = sec.strip()
        if not sec:
            continue

        # Try direct JSON array/object first
        try:
            loaded = json.loads(sec)
            if isinstance(loaded, list):
                for obj in loaded:
                    if isinstance(obj, dict):
                        objs.append(obj)
                print(f"DEBUG parsed as JSON array, got {len(objs)} objects", file=sys.stderr)
                continue
            if isinstance(loaded, dict):
                objs.append(loaded)
                print("DEBUG parsed as single JSON object", file=sys.stderr)
                continue
        except Exception:
            pass

        # Try JSONL (JSON Lines) - each line is a separate JSON object
        jsonl_objs = []
        for ln in sec.splitlines():
            ln = ln.strip()
            if not ln:
                continue
            if not (ln.startswith("{") or ln.startswith("[")):
                continue
            try:
                obj = json.loads(ln)
                if isinstance(obj, dict):
                    jsonl_objs.append(obj)
                elif isinstance(obj, list):
                    jsonl_objs.extend([o for o in obj if isinstance(o, dict)])
            except Exception:
                continue

        if jsonl_objs:
            objs.extend(jsonl_objs)
            print(f"DEBUG parsed as JSONL, got {len(jsonl_objs)} objects", file=sys.stderr)
            continue

        # Try "glued objects" by converting }{ → },{ and wrapping with []
        glued = "[" + re.sub(r"}\s*[\r\n]*\s*{", "},{", sec) + "]"
        try:
            arr = json.loads(glued)
            for obj in arr:
                if isinstance(obj, dict):
                    objs.append(obj)
            print(f"DEBUG parsed as glued objects, got {len(arr)} objects", file=sys.stderr)
            continue
        except Exception:
            pass

        # Last resort: scan braces and parse each fragment
        fragments = split_top_level_json_objects(sec)
        if fragments:
            for frag in fragments:
                try:
                    obj = json.loads(frag)
                    if isinstance(obj, dict):
                        objs.append(obj)
                except Exception:
                    continue
            if objs:
                print(f"DEBUG parsed via brace scanning, got {len(objs)} objects", file=sys.stderr)

    print(f"DEBUG _extract_json_objects total result: {len(objs)} objects", file=sys.stderr)
    return objs


def emit(payload: Any, *, indent: int | None = None) -> None:
    """THE stdout writer: JSON only, UTF-8, nothing else on stdout."""
    sys.stdout.write(json.dumps(payload, ensure_ascii=False, indent=indent))
    sys.stdout.write("\n")
    sys.stdout.flush()
