# -*- coding: utf-8 -*-
"""Final coherence pass — offline stub; LLM optional when key present."""

from __future__ import annotations

import os


def run(*, draft, agent_input, lang="he"):
    notes = []
    rows = (draft or {}).get("Rows") or (draft or {}).get("rows") or []
    if not rows:
        notes.append("draft_has_no_rows")
    # Prompt law: never invent money — reviewer only flags, does not patch totals
    if os.environ.get("OPENAI_API_KEY"):
        notes.append("llm_available_but_offline_stub_used_in_tests")
    return {"Notes": notes, "Patches": []}
