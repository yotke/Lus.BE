# -*- coding: utf-8 -*-
"""
llm_model — ONE place that knows which OpenAI model a script runs on, and how a
given model wants to be called.

Two different call shapes exist and they are NOT interchangeable:

  * chat models      (gpt-4o, gpt-4o-mini) take `temperature` and `max_tokens`.
  * reasoning models (o1/o3/o4/gpt-5)      REJECT both. They take
    `max_completion_tokens`, and they spend hidden reasoning tokens out of that
    SAME budget — so a chat-sized budget returns an empty answer rather than an
    error, which callers then degrade to None. That silent-empty failure is the
    reason `reasoning_headroom()` exists.

Selection is by the OPENAI_MODEL env var (the convention LLM_Constraints.py,
LLM_Conditions_Dynamic.py and llm_rules/cli.py already used). Each caller keeps its
OWN default, so leaving OPENAI_MODEL unset changes nothing anywhere.

The AI Org Builder v2 agents are a separate lane: they read AIB_OPENAI_MODEL, set by
C# from appsettings.json "OpenAI:Model". They share `is_reasoning` from here so the
two lanes can never disagree about what a reasoning model is.
"""
from __future__ import annotations

import os

# Families that reject temperature/max_tokens and bill hidden reasoning tokens.
_REASONING_PREFIXES = ("o1", "o3", "o4", "gpt-5")

# Reasoning-model families that accept the `reasoning_effort` request knob.
# o1 predates it, so it is deliberately absent: sending the kwarg to a model that
# rejects it degrades the WHOLE call to None, silently.
_EFFORT_PREFIXES = ("o3", "o4", "gpt-5")

# Ceiling for the extra output budget reasoning models get, so hidden reasoning does
# not consume the whole allowance and leave an empty visible answer. Tunable without
# a code change via AIB_REASONING_HEADROOM.
# 4000→2000 (08-06): measured usage at effort=low peaks ~700 reasoning tokens per call,
# so 2000 keeps 3× margin while halving the billable output ceiling. If answers start
# coming back EMPTY (the silent failure mode), raise the env var — do not raise this.
_DEFAULT_HEADROOM = 2000

# Headroom is PROPORTIONAL to the visible answer budget (4× it), floored so tiny
# calls still get room to think and capped by the ceiling above. Before this, every
# call got the full flat 4000 — a 120-token chip polish carried a 4120-token billed
# budget, which is where the o4-mini "downgrade" quietly got expensive.
_MIN_HEADROOM = 1000


def is_reasoning(model: str | None) -> bool:
    return (model or "").strip().lower().startswith(_REASONING_PREFIXES)


def reasoning_headroom(max_tokens: int | None = None) -> int:
    try:
        cap = max(0, int(os.environ.get("AIB_REASONING_HEADROOM", _DEFAULT_HEADROOM)))
    except (TypeError, ValueError):
        cap = _DEFAULT_HEADROOM
    if max_tokens is None:
        return cap
    return min(cap, max(_MIN_HEADROOM, 4 * int(max_tokens)))


def reasoning_effort(model: str | None) -> str | None:
    """The `reasoning_effort` to request, or None to omit the kwarg entirely.

    Default is "low": the agents lane asks for shallow extraction/parsing and the
    deterministic layer judges the result, so deep reasoning buys nothing but billed
    hidden tokens. Override with AIB_REASONING_EFFORT (low|medium|high); set it to
    "none" to omit the kwarg. Models outside _EFFORT_PREFIXES never get it — they
    would reject the request and the call would degrade silently to None."""
    name = (model or "").strip().lower()
    if not name.startswith(_EFFORT_PREFIXES):
        return None
    configured = (os.environ.get("AIB_REASONING_EFFORT") or "").strip().lower()
    if configured == "none":
        return None
    if configured in ("low", "medium", "high"):
        return configured
    return "low"


def selected_model(default: str = "gpt-4o-mini") -> str:
    """The model for this process: OPENAI_MODEL if set and non-blank, else the
    caller's own default. A blank env var must never shadow a working default."""
    return (os.environ.get("OPENAI_MODEL") or "").strip() or default


def chat_openai_kwargs(default: str = "gpt-4o-mini",
                       temperature: float | None = None,
                       max_tokens: int | None = None) -> dict:
    """
    kwargs for langchain's ChatOpenAI(...) honouring OPENAI_MODEL, with the
    temperature/token arguments shaped for whichever family got selected. Spread it:

        llm = ChatOpenAI(**chat_openai_kwargs(temperature=0.0))
    """
    model = selected_model(default)
    kwargs: dict = {"model": model}
    if is_reasoning(model):
        if max_tokens is not None:
            kwargs["max_completion_tokens"] = max_tokens + reasoning_headroom(max_tokens)
        return kwargs
    if temperature is not None:
        kwargs["temperature"] = temperature
    if max_tokens is not None:
        kwargs["max_tokens"] = max_tokens
    return kwargs
