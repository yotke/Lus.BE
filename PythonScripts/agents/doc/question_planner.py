# -*- coding: utf-8 -*-
"""
Pick the single next question that moves the document closest to signable.

Deterministic on purpose: which gap matters most is a property of the document, not a
judgement call, and an interview that asks in a stable order is one the user can learn.
The ladder runs identity -> content -> per-row gaps -> billing, because answering the
billing questions first would price a document that has nothing in it yet.

Every question carries an Id the C# QuestionAnswerBinder maps back onto a draft field, so
the answer lands where it was asked for instead of being re-parsed as dictation.
"""

from __future__ import annotations

# Rows the user is unlikely to describe by hand — offered as chips instead.
COMMON_LOCATIONS = ["משרד", "שטח", 'רש"ת']
DEFAULT_VAT = "18"


def _rows(draft):
    return (draft or {}).get("Rows") or (draft or {}).get("rows") or []


def _totals(draft):
    return (draft or {}).get("Totals") or (draft or {}).get("totals") or {}


def _template(draft):
    return (draft or {}).get("Template") or (draft or {}).get("template") or {}


def _pick(source, *names):
    for name in names:
        value = (source or {}).get(name)
        if value not in (None, ""):
            return value
    return None


def _blank(value):
    return value in (None, "", 0, 0.0)


def _q(qid, text, chips=None):
    return {"Question": {"Id": qid, "Text": text, "Chips": chips or []}}


def _known_locations(rows):
    """Chips grounded in what this document already uses, then the usual suspects."""
    seen = []
    for row in rows:
        location = _pick(row, "Location", "location")
        if location and location not in seen:
            seen.append(location)
    for fallback in COMMON_LOCATIONS:
        if fallback not in seen:
            seen.append(fallback)
    return seen[:4]


def _first_row_gap(rows):
    """
    The first row missing something, and what it is missing.

    Ordered date -> hours -> subject -> location: a row without a date cannot be placed in
    the month, and one without hours cannot be billed, so those are asked about first.
    """
    for index, row in enumerate(rows):
        if _blank(_pick(row, "Date", "date")):
            return index, "date"
        if _blank(_pick(row, "Hours", "hours")):
            return index, "hours"
        if _blank(_pick(row, "Subject", "subject")):
            return index, "subject"
        if _blank(_pick(row, "Location", "location")):
            return index, "location"
    return None, None


def _describe(row, index):
    """Name the row the way the user sees it, so the question is unambiguous."""
    date = _pick(row, "Date", "date")
    subject = _pick(row, "Subject", "subject")
    if date:
        return str(date)[:10]
    if subject:
        return str(subject)[:30]
    return f"#{index + 1}"


def run(*, draft, agent_input, lang="he"):
    draft = draft or {}
    rows = _rows(draft)
    totals = _totals(draft)
    template = _template(draft)

    account = _pick(draft, "AccountNumber", "accountNumber")
    client = _pick(template, "ClientName", "clientName")
    rate = _pick(totals, "HourlyRate", "hourlyRate")
    carry_in = _pick(totals, "CarryIn", "carryIn")
    vat = _pick(totals, "VatPercent", "vatPercent")

    # ── 1. Identity: who the report is for and under which account ────────────────
    if not client:
        return _q("client_name", "למי מופנה הדוח? (שם הלקוח)")

    if not account:
        return _q("account_number", "מה מספר החשבון של הדוח?")

    # ── 2. Content before money ───────────────────────────────────────────────────
    if not rows:
        return _q(
            "first_row",
            "מה השורה הראשונה ביומן העבודה?",
            ["3 שעות במשרד — התייעצות", "2 שעות בשטח — סיור"],
        )

    # ── 3. Finish the rows that are already there ─────────────────────────────────
    index, missing = _first_row_gap(rows)
    if missing == "date":
        return _q(f"row_date:{index}", f"מה התאריך של שורה {_describe(rows[index], index)}?")
    if missing == "hours":
        return _q(
            f"row_hours:{index}",
            f"כמה שעות נרשמו בשורה {_describe(rows[index], index)}?",
            ["1", "2", "3", "4"],
        )
    if missing == "subject":
        return _q(f"row_subject:{index}", f"מה נושא העבודה בשורה {_describe(rows[index], index)}?")
    if missing == "location":
        return _q(
            f"row_location:{index}",
            f"היכן בוצעה העבודה בשורה {_describe(rows[index], index)}?",
            _known_locations(rows),
        )

    # ── 4. The series: this month continues the previous one ──────────────────────
    if carry_in in (None, ""):
        return _q("carry_in", "מה היתרה שנותרה מהדוח הקודם?", ["0"])

    # ── 5. Billing, last — a document with content is worth pricing ───────────────
    if _blank(rate):
        return _q("hourly_rate", "מה התעריף לשעת עבודה?", ["225", "223.97"])

    if _blank(vat):
        return _q("vat_percent", 'מה שיעור המע"מ באחוזים?', [DEFAULT_VAT])

    # Nothing left worth asking: the document can be issued.
    return {"Question": None}
