# -*- coding: utf-8 -*-
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

from agents.runner import _registry  # noqa: E402
from pyutil.model_router import AGENT_TIERS  # noqa: E402

DETERMINISTIC = {
    "echo",
    "template_reader",
    "carry_forward",
    "formatter",
    "validator",
    "question_planner",
}


def test_every_llm_registry_agent_has_a_tier_row():
    for name in _registry():
        if name in DETERMINISTIC:
            continue
        assert name in AGENT_TIERS, f"{name} is LLM-bearing but has no AGENT_TIERS row"
