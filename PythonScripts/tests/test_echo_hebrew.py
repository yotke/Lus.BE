# -*- coding: utf-8 -*-
import json
import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def _run(agent: str, payload: str, extra_env=None):
    env = os.environ.copy()
    if extra_env:
        env.update(extra_env)
    return subprocess.run(
        [sys.executable, "agents/runner.py", "--agent", agent, "--lang", "he",
         "--non-interactive", "--payload-stdin"],
        cwd=ROOT,
        input=payload,
        text=True,
        capture_output=True,
        env=env,
        encoding="utf-8",
    )


def _envelope(completed: subprocess.CompletedProcess) -> dict:
    assert completed.returncode == 0, completed.stderr
    line = completed.stdout.strip().splitlines()[-1]
    return json.loads(line)


def test_echo_hebrew():
    env = _envelope(_run("doc.echo", json.dumps({"Draft": {}, "Input": {"Text": "שלום עולם"}}, ensure_ascii=False)))
    assert env["Ok"] is True
    assert env["Agent"] == "doc.echo"
    assert env["Result"]["Echo"] == "שלום עולם"


def test_echo_hebrew_with_bom():
    payload = "\ufeff" + json.dumps({"Draft": {}, "Input": {"Text": "שלום"}}, ensure_ascii=False)
    env = _envelope(_run("doc.echo", payload))
    assert env["Ok"] is True
    assert env["Result"]["Echo"] == "שלום"


def test_unknown_agent_safe_envelope():
    env = _envelope(_run("doc.nope", "{}"))
    assert env["Ok"] is False
    assert env["ErrorInfo"]["Code"] == "unknown_agent"
