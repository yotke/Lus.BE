# -*- coding: utf-8 -*-
"""Work-log Hebrew prose → row patches. Deterministic regex lane (no LLM required)."""

from __future__ import annotations

import re
from datetime import datetime

# Hebrew month names → month number (no year keyword lists in orchestrator — this is parsing only)
_HE_MONTHS = {
    "ינואר": 1, "פברואר": 2, "מרץ": 3, "מרס": 3, "אפריל": 4,
    "מאי": 5, "יוני": 6, "יולי": 7, "אוגוסט": 8, "ספטמבר": 9,
    "אוקטובר": 10, "נובמבר": 11, "דצמבר": 12,
}

_HOURS_RE = re.compile(r"(\d+(?:\.\d+)?)\s*שע")
_DATE_HE_RE = re.compile(r"(\d{1,2})\s+ב?(ינואר|פברואר|מרץ|מרס|אפריל|מאי|יוני|יולי|אוגוסט|ספטמבר|אוקטובר|נובמבר|דצמבר)")
_DATE_ISO_RE = re.compile(r"(\d{1,2})[\./](\d{1,2})[\./](\d{2,4})")
_LOCATION_RE = re.compile(r"ב(משרד|שטח|בית|אתר|זום)")


def _parse_date(text: str, default_year: int = 2026) -> str | None:
    m = _DATE_HE_RE.search(text)
    if m:
        day = int(m.group(1))
        month = _HE_MONTHS.get(m.group(2), 1)
        return f"{default_year:04d}-{month:02d}-{day:02d}"
    m = _DATE_ISO_RE.search(text)
    if m:
        d, mo, y = int(m.group(1)), int(m.group(2)), int(m.group(3))
        if y < 100:
            y += 2000
        return f"{y:04d}-{mo:02d}-{d:02d}"
    return None


def _parse_hours(text: str) -> float | None:
    m = _HOURS_RE.search(text)
    return float(m.group(1)) if m else None


def _parse_location(text: str) -> str | None:
    m = _LOCATION_RE.search(text)
    return m.group(1) if m else None


def _subject(text: str) -> str:
    # strip leading date/hours/location fragments; remainder is subject
    t = _DATE_HE_RE.sub("", text)
    t = _DATE_ISO_RE.sub("", t)
    t = _HOURS_RE.sub("", t)
    t = _LOCATION_RE.sub("", t)
    t = re.sub(r"^[\s\-–—:,]+", "", t)
    return t.strip() or text.strip()


def run(*, draft, agent_input, lang="he"):
    text = (agent_input or {}).get("Text") or (agent_input or {}).get("text") or ""
    text = text.strip()
    if not text:
        return {"Patches": [], "Notes": ["empty_input"]}

    date = _parse_date(text)
    hours = _parse_hours(text)
    location = _parse_location(text)
    subject = _subject(text)

    if hours is None:
        return {"Patches": [], "Notes": ["missing_hours"]}

    row = {
        "Date": date,
        "Hours": hours,
        "Location": location,
        "Subject": subject,
    }
    if date:
        try:
            row["DayOfWeek"] = datetime.fromisoformat(date).weekday()
        except ValueError:
            pass

    return {
        "Patches": [{"Op": "AddRow", "Path": "rows", "Value": row}],
        "Notes": [],
    }
