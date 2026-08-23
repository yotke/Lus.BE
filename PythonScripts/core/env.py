"""Canonical environment/API-key handling (moved from llm_rules/env.py, Phase 2).

Every entry script uses THESE functions — per-script reimplementations were
removed (they lived in LLM_TransliterateName/LLM_AnalyticsInsights/
LLM_SmartSubdomain/LLM_CreateShift/LLM_DayEdit and inline in 4 more).
"""
import os
import sys
from typing import Optional


def log_debug(msg: str) -> None:
    # Everything important goes to stderr so the C# adapter can read it;
    # stdout is reserved for the JSON result payload.
    print(f"DEBUG {msg}", file=sys.stderr)


def load_dotenv_manual(path: str) -> None:
    try:
        with open(path, encoding="utf-8") as fh:
            for ln in fh:
                if "=" in ln and not ln.lstrip().startswith("#"):
                    k, v = ln.strip().split("=", 1)
                    if k and k not in os.environ:
                        os.environ[k] = v.strip().strip('"').strip("'")
    except FileNotFoundError:
        pass


def ensure_api_key(cli_key: Optional[str]) -> str:
    key = (cli_key or os.getenv("OPENAI_API_KEY", "")).strip()
    if not key:
        raise ValueError("Missing OpenAI API key (arg or OPENAI_API_KEY).")
    os.environ["OPENAI_API_KEY"] = key  # make sure child libs see it
    log_debug(f"Using API key: {key[:8]}…")
    return key


def gemini_config() -> dict:
    """Gemini provider config — env-only by doctrine (never argv; see plan
    child 03/07: keys must not appear in `ps` output). C# passes these when
    `Gemini:*` is configured (appsettings) via the process-runner env lane."""
    return {
        "api_key": os.getenv("GEMINI_API_KEY", "").strip(),
        "model": os.getenv("GEMINI_MODEL", "").strip(),
        "base_url": os.getenv("GEMINI_BASE_URL", "").strip(),
    }
