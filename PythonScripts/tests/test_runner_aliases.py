# -*- coding: utf-8 -*-
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

from agents.runner import AGENT_ALIASES, _registry  # noqa: E402
from pyutil.model_router import AGENT_TIERS  # noqa: E402

DETERMINISTIC = {
    "echo",
    "template_reader",
    "carry_forward",
    "formatter",
    "validator",
    "question_planner",
}


def test_every_alias_targets_a_registered_agent():
    registry = _registry()
    for alias, canonical in AGENT_ALIASES.items():
        assert canonical in registry, f"{alias} → {canonical} is not registered"
        prefix = alias.split(".", 1)[0]
        assert prefix == "doc", f"{alias} must be namespaced doc.*"


def test_doc_star_lives_under_agents_doc():
    for alias in AGENT_ALIASES:
        assert alias.startswith("doc.")


def test_llm_agents_in_registry_have_tier_rows():
    registry = _registry()
    for name in registry:
        if name in DETERMINISTIC:
            continue
        assert name in AGENT_TIERS, f"LLM agent {name} missing pyutil.model_router.AGENT_TIERS row"
