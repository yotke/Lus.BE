# -*- coding: utf-8 -*-
"""Grounded observations about the draft. Deterministic, and silent when it has nothing to say."""

from __future__ import annotations


def _rows(draft):
    return (draft or {}).get("Rows") or (draft or {}).get("rows") or []


def _totals(draft):
    return (draft or {}).get("Totals") or (draft or {}).get("totals") or {}


def _get(totals, *names):
    for name in names:
        if totals.get(name) is not None:
            return totals[name]
    return None


def run(*, draft, agent_input, lang="he"):
    """
    Returns at most ONE grounded observation about the document.

    It used to echo the user's own message back at them ("יש N שורות בטיוטה. שאלה: <input>"),
    which read as the assistant not understanding anything. An advisor with nothing useful to
    say should say nothing: the orchestrator drops an empty Answer, so the chat stays clean.
    """
    rows = _rows(draft)
    totals = _totals(draft)
    he = lang == "he"

    rate = _get(totals, "HourlyRate", "hourlyRate")
    hours = _get(totals, "Hours", "hours") or 0
    remaining = _get(totals, "Remaining", "remaining")

    # Never restate what the planner is already asking for — that is the question's job.
    if not rows:
        return {"Answer": "", "Suggestions": []}

    if remaining is not None and remaining < 0:
        answer = (
            f"שים לב: נרשמו {hours} שעות אך היתרה הקודמת קטנה מכך — היתרה יוצאת {remaining}."
            if he else
            f"Note: {hours} hours recorded but the carried-in balance is smaller — remaining is {remaining}."
        )
        return {"Answer": answer, "Suggestions": []}

    if rate is not None and hours:
        total = round(float(hours) * float(rate), 2)
        answer = (
            f"{hours} שעות בתעריף {rate} — סה\"כ {total} לפני מע\"מ."
            if he else
            f"{hours} hours at {rate} — {total} before VAT."
        )
        return {"Answer": answer, "Suggestions": []}

    return {"Answer": "", "Suggestions": []}
