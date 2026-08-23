#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Lus Document Builder — generic agent runner.

Invocation (from C# PythonScriptsAdapter.RunAgentAsync):
    python agents/runner.py --agent doc.echo --lang he --non-interactive --payload-stdin [--api-key K]

Payload arrives on STDIN as one JSON doc: {"Draft": <draft>, "Input": <input>}.
(STDIN instead of --*-json argv: drafts grow with the session and Windows caps
a command line at ~32K chars.)

Output: ONE line of PascalCase JSON on stdout —
    {"Ok": true,  "Agent": "...", "SchemaVersion": 1, "Result": {...}, "ErrorInfo": null}
    {"Ok": false, "Agent": "...", "SchemaVersion": 1, "Result": null,
     "ErrorInfo": {"Code": "...", "UserMessage": "<he>", "UserMessageEn": "<en>"}}

SECURITY: this script NEVER exposes internal errors to users. Tracebacks go to
stderr only; stdout always gets a safe envelope and the exit code is ALWAYS 0
for handled failures — a non-zero exit means the interpreter itself crashed
and C# raises PythonScriptException.
"""
from __future__ import annotations

import argparse
import json
import os
import sys

SCHEMA_VERSION = 1

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
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, ValueError):
        pass
    print(json.dumps(envelope, ensure_ascii=False))


def _load_schema(agent: str):
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "schemas", f"{agent}.result.schema.json")
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def _validate_result(agent: str, result: dict) -> str | None:
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


# Namespaced spellings resolve to the canonical flat registry key.
# Envelope echoes the REQUESTED name back.
AGENT_ALIASES = {
    "doc.echo": "echo",
    "doc.template_reader": "template_reader",
    "doc.carry_forward": "carry_forward",
    "doc.schema_planner": "schema_planner",
    "doc.row_extractor": "row_extractor",
    "doc.formatter": "formatter",
    "doc.validator": "validator",
    "doc.reviewer": "reviewer",
    "doc.question_planner": "question_planner",
    "doc.advisor": "advisor",
}


def _registry():
    from agents.doc.advisor import run as advisor
    from agents.doc.carry_forward import run as carry_forward
    from agents.doc.echo import run as echo
    from agents.doc.formatter import run as formatter
    from agents.doc.question_planner import run as question_planner
    from agents.doc.reviewer import run as reviewer
    from agents.doc.row_extractor import run as row_extractor
    from agents.doc.schema_planner import run as schema_planner
    from agents.doc.template_reader import run as template_reader
    from agents.doc.validator import run as validator
    return {
        "advisor": advisor,
        "carry_forward": carry_forward,
        "echo": echo,
        "formatter": formatter,
        "question_planner": question_planner,
        "reviewer": reviewer,
        "row_extractor": row_extractor,
        "schema_planner": schema_planner,
        "template_reader": template_reader,
        "validator": validator,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Lus Document Builder agent runner")
    parser.add_argument("--agent", required=True)
    parser.add_argument("--lang", default="en")
    parser.add_argument("--api-key", "-k", default="")
    parser.add_argument("--non-interactive", action="store_true")
    parser.add_argument("--payload-stdin", action="store_true")
    args = parser.parse_args()

    agent = args.agent
    canonical = AGENT_ALIASES.get(agent, agent)
    os.environ["AIB_AGENT_NAME"] = canonical

    try:
        if args.api_key:
            os.environ["OPENAI_API_KEY"] = args.api_key

        registry = _registry()
        if canonical not in registry:
            _emit(_safe_error(agent, "unknown_agent",
                              "סוכן לא מוכר.", f"Unknown agent '{agent}'."))
            sys.exit(0)

        if args.payload_stdin:
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
                              "הסוכן החזיר תוצאה לא תקינה.", "The agent returned an invalid result."))
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
        sys.exit(0)


if __name__ == "__main__":
    main()
