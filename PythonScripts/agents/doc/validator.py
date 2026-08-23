# -*- coding: utf-8 -*-
"""Coherence lint + deterministic auto-fixes. Empty rate blocks commit."""

from __future__ import annotations

from datetime import datetime


def _rows(draft) -> list:
    draft = draft or {}
    return list(draft.get("Rows") or draft.get("rows") or [])


def _totals(draft) -> dict:
    draft = draft or {}
    return dict(draft.get("Totals") or draft.get("totals") or {})


def run(*, draft, agent_input, lang="he"):
    warnings: list[dict] = []
    patches: list[dict] = []
    rows = _rows(draft)
    totals = _totals(draft)

    rate = totals.get("HourlyRate") if totals.get("HourlyRate") is not None else totals.get("hourlyRate")
    if rate in (None, "", 0, 0.0):
        warnings.append({
            "Code": "empty_rate",
            "Message": "חסר תעריף שעתי — לא ניתן להפיק חשבון.",
        })

    seen = set()
    for i, row in enumerate(rows):
        subj = (row.get("Subject") or row.get("subject") or "").strip()
        date = row.get("Date") or row.get("date")
        key = (str(date), subj)
        if subj and key in seen:
            warnings.append({
                "Code": "duplicate_row",
                "Message": f"שורה כפולה: {subj}",
                "Path": f"rows[{i}]",
            })
        seen.add(key)

        # Auto-fix day-of-week from date — never trust typed day
        if date:
            try:
                if isinstance(date, str):
                    dt = datetime.fromisoformat(date.replace("Z", "")[:10])
                else:
                    dt = date
                derived = dt.weekday()
                current = row.get("DayOfWeek") if row.get("DayOfWeek") is not None else row.get("dayOfWeek")
                if current != derived:
                    patches.append({
                        "Op": "UpdateRow",
                        "Path": f"rows[{i}].dayOfWeek",
                        "Value": {"DayOfWeek": derived, "Date": date if isinstance(date, str) else dt.isoformat()[:10]},
                    })
            except (ValueError, TypeError):
                warnings.append({"Code": "bad_date", "Message": f"תאריך לא תקין בשורה {i + 1}"})

    ok = not any(w.get("Code") == "empty_rate" for w in warnings)
    return {
        "Ok": ok,
        "Warnings": warnings,
        "Patches": patches,
    }
