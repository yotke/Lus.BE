"""
Shared helper for emitting per-LLM-call cost lines that the C# MeteredPythonExecutor
parses out of STDERR. See `.brain/payment-billing/03-ai-python-and-metering-flows.md`
and `.brain/payment-billing/08-ai-dynamic-pricing.md` §5.

Usage in any LLM_*.py script:

    from pyutil.credits import emit_cost
    ...
    response = openai.chat.completions.create(...)
    emit_cost(
        input_tokens=response.usage.prompt_tokens,
        output_tokens=response.usage.completion_tokens,
        model=response.model,
        items=1,
    )

The line written to STDERR is:
    COST:{"input_tokens":1234,"output_tokens":456,"model":"gpt-4o-mini","items":1}\n

STDOUT remains untouched so the existing JSON-on-STDOUT contract is preserved.
"""
from __future__ import annotations

import json
import os
import sys
from typing import Any, Mapping


def emit_cost(
    input_tokens: int,
    output_tokens: int,
    model: str,
    items: int = 1,
    extra: Mapping[str, Any] | None = None,
) -> None:
    """Write a single COST: line to STDERR. Idempotent at call-site only."""
    payload: dict[str, Any] = {
        "input_tokens": int(input_tokens),
        "output_tokens": int(output_tokens),
        "model": str(model),
        "items": int(items),
    }
    if extra:
        # never override the canonical keys
        for k, v in extra.items():
            if k not in payload:
                payload[k] = v
    line = "COST:" + json.dumps(payload, separators=(",", ":")) + "\n"
    try:
        sys.stderr.write(line)
        sys.stderr.flush()
    except Exception:
        # cost emission must NEVER crash the script
        pass


def is_draft_quality() -> bool:
    """
    True when the C# MeteredPythonExecutor wants this script to use the cheaper
    model / smaller max_tokens / fewer retries (ReducedAccuracyPolicy degradation
    mode). See `.brain/payment-billing/06-freemium-strategy.md` §5.
    """
    return os.environ.get("ARMYLUZ_QUALITY", "").lower() == "draft"


def reservation_key() -> str | None:
    """The reservation key issued by C# CreditEngineService.ReserveAsync, if any."""
    return os.environ.get("ARMYLUZ_RESERVATION_KEY") or None


def correlation_id() -> str | None:
    return os.environ.get("ARMYLUZ_CORRELATION_ID") or None

