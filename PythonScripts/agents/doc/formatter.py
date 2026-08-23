# -*- coding: utf-8 -*-
"""LLM-free totals, carry-in, remaining, billing. Never invents money."""

from __future__ import annotations

from decimal import Decimal, ROUND_HALF_UP


def _d(x) -> Decimal:
    if x is None:
        return Decimal("0")
    return Decimal(str(x))


def _money(x: Decimal) -> float:
    return float(x.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP))


def run(*, draft, agent_input, lang="he"):
    draft = draft or {}
    rows = draft.get("Rows") or draft.get("rows") or []
    totals = draft.get("Totals") or draft.get("totals") or {}

    hours = sum(_d(r.get("Hours") or r.get("hours")) for r in rows)
    carry_in = _d(totals.get("CarryIn") or totals.get("carryIn") or 0)
    remaining = carry_in - hours
    hourly = totals.get("HourlyRate") if totals.get("HourlyRate") is not None else totals.get("hourlyRate")
    vat = _d(totals.get("VatPercent") or totals.get("vatPercent") or 18)
    plots = totals.get("PlotsPercent") if totals.get("PlotsPercent") is not None else totals.get("plotsPercent")

    total_bill = None
    if hourly is not None and hourly != "":
        subtotal = _d(hourly) * hours
        if plots is not None and plots != "":
            subtotal -= subtotal * (_d(plots) / Decimal("100"))
        total_bill = _money(subtotal * (Decimal("1") + vat / Decimal("100")))

    next_totals = {
        "Hours": _money(hours),
        "CarryIn": _money(carry_in),
        "Remaining": _money(remaining),
        "HourlyRate": float(hourly) if hourly not in (None, "") else None,
        "VatPercent": float(vat),
        "PlotsPercent": float(plots) if plots not in (None, "") else None,
        "Total": total_bill,
    }

    patches = [{"Op": "SetTotals", "Path": "totals", "Value": next_totals}]
    return {"Patches": patches, "Totals": next_totals}
