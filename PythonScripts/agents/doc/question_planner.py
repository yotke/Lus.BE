# -*- coding: utf-8 -*-
"""Pick the single next interview question (deterministic)."""

from __future__ import annotations


def run(*, draft, agent_input, lang="he"):
    draft = draft or {}
    rows = draft.get("Rows") or draft.get("rows") or []
    totals = draft.get("Totals") or draft.get("totals") or {}
    rate = totals.get("HourlyRate") if totals.get("HourlyRate") is not None else totals.get("hourlyRate")

    if not rows:
        return {
            "Question": {
                "Id": "first_row",
                "Text": "מה השורה הראשונה ביומן העבודה?",
                "Chips": ["3 שעות במשרד — התייעצות", "2 שעות בשטח — סיור"],
            }
        }

    if rate in (None, "", 0, 0.0):
        return {
            "Question": {
                "Id": "hourly_rate",
                "Text": "מה התעריף לשעת עבודה?",
                "Chips": ["225", "223.97"],
            }
        }

    return {"Question": None}
