# -*- coding: utf-8 -*-
"""
model_router — the ONE selection layer between every builder LLM call and a model.

Agents declare a CAPABILITY tier centrally here (never in agent files); the policy
picks the cheapest model that satisfies it, shifting DOWN for trivial inputs and UP
only when a deterministic complexity SCORE crosses the deep threshold. The model ids
themselves are configuration (env, set by C# from appsettings "OpenAI:Model*") —
never Python literals.

Laws (docs/BUILDER_MODEL_ROUTING.md):
  * Parity when off — with none of the tier envs set, every route returns the legacy
    AIB_OPENAI_MODEL exactly as before this layer existed.
  * The code judges the route — the score is regex/counting only, no LLM, no domain
    lexicons beyond GENERIC linguistic markers. Length alone can never reach deep.
  * Escalation is content-capability only; 90-95% of calls must never reach deep.
  * Ladder before deep — an empty/invalid cheap-tier answer earns ONE retry a single
    tier up (the provider owns that loop; ladder_up here computes the step).
  * Budget-aware — AIB_BUDGET_STATE=warning prefers one tier down, critical caps at
    chat; unset means healthy.
Shared by BOTH lanes (agents/* via AIB_AGENT_NAME and CreateOrganization/
org_extractor.py directly), like pyutil.llm_model — so the lanes can never disagree.
"""
from __future__ import annotations

import os
import re
from dataclasses import dataclass

from pyutil.llm_model import is_reasoning, reasoning_effort, reasoning_headroom

TIER_LITE = "lite"        # AIB_MODEL_LITE    — trivial single-fact calls
TIER_CHAT = "chat"        # AIB_MODEL_CHAT    — main chat flows
TIER_CONTENT = "content"  # AIB_MODEL_CONTENT — extraction + text generation
TIER_DEEP = "deep"        # AIB_MODEL_DEEP    — score-gated escalation only

_TIER_ENV = {
    TIER_LITE: "AIB_MODEL_LITE",
    TIER_CHAT: "AIB_MODEL_CHAT",
    TIER_CONTENT: "AIB_MODEL_CONTENT",
    TIER_DEEP: "AIB_MODEL_DEEP",
}
_LADDER = (TIER_LITE, TIER_CHAT, TIER_CONTENT, TIER_DEEP)

# agent → capability tier. DRIFT ALARM: tests/test_model_router.py asserts every
# LLM-bearing agent in agents/runner.py's registry has a row here.
# Deterministic agents (echo, formatter, validator, carry_forward, template_reader,
# question_planner) never call the LLM and need no row.
AGENT_TIERS = {
    "advisor": TIER_CHAT,
    "schema_planner": TIER_CONTENT,
    "row_extractor": TIER_CONTENT,
    "reviewer": TIER_CONTENT,
}

# content-capability callers whose work is EXTRACTION (deep-eligible).
DEEP_ELIGIBLE = {
    "schema_planner", "row_extractor", "reviewer",
}

_LEGACY_DEFAULT = "gpt-4o"  # mirrors openai_provider's historical fallback


@dataclass(frozen=True)
class Route:
    model: str
    tier: str
    reason: str


def _legacy_model() -> str:
    return (os.environ.get("AIB_OPENAI_MODEL") or "").strip() or _LEGACY_DEFAULT


def tiers_configured() -> bool:
    """True when ANY tier env is set — otherwise the layer is OFF (parity law)."""
    return any((os.environ.get(v) or "").strip() for v in _TIER_ENV.values())


def _tier_model(tier: str) -> str:
    """The configured model for a tier; an unset tier falls back to the legacy
    model (a blank env var must never shadow a working default)."""
    return (os.environ.get(_TIER_ENV[tier]) or "").strip() or _legacy_model()


def _int_env(name: str, default: int) -> int:
    try:
        return int(os.environ.get(name, default))
    except (TypeError, ValueError):
        return default


# ---------------------------------------------------------------------------
# Complexity score — deterministic, generic, tunable. User rubric 08-06:
# constraints/conflicts/planning/reasoning-shaped signals weigh +2, size/density
# signals weigh +1. Length signals alone total +2 and can NEVER reach the deep
# threshold (8) — that invariant is pinned by tests.
# ---------------------------------------------------------------------------
_CLOCK_RE = re.compile(r"\b\d{1,2}:\d{2}\b")
_NUMBER_RE = re.compile(r"\d+")
_LIST_LINE_RE = re.compile(r"^\s*(?:[-*•]|\d+[.)])\s+", re.MULTILINE)
# GENERIC linguistic markers only (multilingual), never domain lexicons:
_OBLIGATION_RE = re.compile(
    r"\b(?:must|always|never|only|forbidden|required|חייב|חייבת|חובה|אסור|תמיד|בלבד)\b",
    re.IGNORECASE)
_EXCEPTION_RE = re.compile(
    r"\b(?:except|unless|besides|למעט|מלבד|אלא)\b|חוץ מ", re.IGNORECASE)
_CONDITIONAL_RE = re.compile(
    r"\b(?:if|when|whenever|בתנאי|אם|כאשר|כשיש)\b", re.IGNORECASE)
# Plan 19 Phase 5 — rule-CONNECTION reasoning (dependencies, conflicts,
# alternatives, combinations) is exactly the work the user reserved for the
# expensive tier; plain extraction stays on the mini. Generic signals, not
# domain lexicons (the parse/judge law).
_RELATION_RE = re.compile(
    r"\b(?:depends?|conflicts?|alternatively|alternative|instead of|together with|"
    r"combined|mutually|תלוי|תלויה|תלויים|מתנגש|מתנגשת|מתנגשים|חלופה|חלופי|חלופית|"
    r"במקום ש|יחד עם|משולב|משולבת|סותר|סותרת)\b", re.IGNORECASE)


def complexity_score(text: str) -> int:
    t = text or ""
    score = 0
    if len(t) >= _int_env("AIB_DEEP_MIN_CHARS", 300):
        score += 1                                   # long context
    if len(t) >= 900:
        score += 1                                   # very long context
    if len(_CLOCK_RE.findall(t)) >= 2:
        score += 1                                   # time windows
    if len(_NUMBER_RE.findall(t)) >= 6:
        score += 1                                   # counts/calculations
    list_lines = len(_LIST_LINE_RE.findall(t))
    if list_lines >= 5:
        score += 1                                   # entity/list density
    if list_lines >= 12:
        score += 1
    if len(_OBLIGATION_RE.findall(t)) >= 2:
        score += 2                                   # multiple constraints
    if _EXCEPTION_RE.search(t):
        score += 2                                   # conflicting/exception requirements
    if len(_CONDITIONAL_RE.findall(t)) >= 2:
        score += 2                                   # conditional planning
    if _RELATION_RE.search(t):
        score += 2                                   # rule connections/dependencies (plan 19)
    return score


def _budget_cap(tier: str) -> tuple[str, str]:
    """(possibly-lowered tier, note). warning → one tier down for deep/content;
    critical → cap at chat. Unset/unknown = healthy = unchanged."""
    state = (os.environ.get("AIB_BUDGET_STATE") or "").strip().lower()
    if state == "warning" and tier in (TIER_DEEP, TIER_CONTENT):
        idx = _LADDER.index(tier)
        return _LADDER[idx - 1], "+budget:warning"
    if state == "critical" and tier in (TIER_DEEP, TIER_CONTENT):
        return TIER_CHAT, "+budget:critical"
    return tier, ""


def pick(agent: str | None, text: str = "", *, allow_deep: bool | None = None,
         fallback: str | None = None) -> Route:
    """Route for one LLM call. Resolution: per-agent override > parity fallback >
    capability tier (score-shifted for content) > budget cap > tier env model.

    `fallback` is the caller's own historical model for the layer-off/unknown cases —
    org_extractor always hardcoded gpt-4o-mini and deliberately ignored
    AIB_OPENAI_MODEL, so ITS parity model is that literal, not the agents-lane one."""
    name = (agent or "").strip()
    override = (os.environ.get(f"AIB_MODEL__{name.upper()}") or "").strip() if name else ""
    if override:
        return Route(override, "override", f"override:{name}")

    legacy = (fallback or "").strip() or _legacy_model()
    if not tiers_configured():
        return Route(legacy, "legacy", "tiers-unset")

    tier = AGENT_TIERS.get(name)
    if tier is None:
        return Route(legacy, "legacy", f"unknown-agent:{name or '?'}")

    reason = f"cap:{tier}"
    if tier == TIER_CONTENT:
        score = complexity_score(text)
        deep_ok = DEEP_ELIGIBLE.__contains__(name) if allow_deep is None else allow_deep
        # Plan 19 Phase 5 tightening (user cost report 08-08): the old bar of 4 sent
        # ROUTINE briefs to the content model — long text (+1) + two clock times (+1)
        # + six numbers (+1) + one "חייב" (+2) is an ordinary staffing sentence, not
        # complex reasoning. gpt-5-mini is the workhorse; the content model needs
        # REAL evidence (score ≥ 7: dense constraints, exceptions, conditionals,
        # rule connections), deep stays at 8+. Both remain env-tunable.
        if score < _int_env("AIB_SCORE_CONTENT", 7):
            tier, reason = TIER_CHAT, f"score:{score}<content"
        elif deep_ok and score >= _int_env("AIB_SCORE_DEEP", 8):
            tier, reason = TIER_DEEP, f"score:{score}>=deep"
        else:
            reason = f"score:{score}"

    tier, budget_note = _budget_cap(tier)
    return Route(_tier_model(tier), tier, reason + budget_note)


def ladder_up(route: Route, agent: str | None = None) -> Route | None:
    """ONE step up for an empty/invalid answer (the provider retries exactly once).
    Never runs when the layer is off, never steps into deep for a caller that is
    not deep-eligible, and never moves past the top."""
    if not tiers_configured() or route.tier not in _LADDER:
        return None
    idx = _LADDER.index(route.tier)
    if idx + 1 >= len(_LADDER):
        return None
    nxt = _LADDER[idx + 1]
    name = (agent or "").strip()
    if nxt == TIER_DEEP and name not in DEEP_ELIGIBLE:
        return None
    return Route(_tier_model(nxt), nxt, f"ladder+1:{route.tier}->{nxt}")


def chat_kwargs(route: Route, temperature: float | None = None,
                max_tokens: int | None = None) -> dict:
    """kwargs for a RAW client.chat.completions.create call, shaped for the routed
    model's family (reasoning models reject temperature/max_tokens and need
    headroom + effort — the silent-empty law). Spread it:

        client.chat.completions.create(**chat_kwargs(route, 0.3, 1000), messages=…)
    """
    kwargs: dict = {"model": route.model}
    if is_reasoning(route.model):
        if max_tokens is not None:
            kwargs["max_completion_tokens"] = max_tokens + reasoning_headroom(max_tokens)
        effort = deep_effort(route.model) if route.tier == TIER_DEEP else reasoning_effort(route.model)
        if effort is not None:
            kwargs["reasoning_effort"] = effort
        return kwargs
    if temperature is not None:
        kwargs["temperature"] = temperature
    if max_tokens is not None:
        kwargs["max_tokens"] = max_tokens
    return kwargs


def deep_effort(model: str) -> str | None:
    """Deep-tier effort: AIB_REASONING_EFFORT_DEEP (low|medium|high|none) so o3 can
    think harder without touching the cheap tiers; falls back to the shared
    AIB_REASONING_EFFORT resolution."""
    if reasoning_effort(model) is None:  # family rejects the kwarg — never send it
        return None
    configured = (os.environ.get("AIB_REASONING_EFFORT_DEEP") or "").strip().lower()
    if configured == "none":
        return None
    if configured in ("low", "medium", "high"):
        return configured
    return reasoning_effort(model)
