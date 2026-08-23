# -*- coding: utf-8 -*-
"""Clone last month: clear data band, repoint chain, seed letterhead."""

from __future__ import annotations


def run(*, draft, agent_input, lang="he"):
    draft = draft or {}
    inp = agent_input or {}
    period = inp.get("Period") or inp.get("period") or {}
    sheet_name = period.get("SheetName") or period.get("sheetName") or "חודש חדש"
    account = draft.get("AccountNumber") or draft.get("accountNumber") or inp.get("AccountNumber")

    patches = [
        {"Op": "SetField", "Path": "rows", "Value": []},
        {"Op": "SetField", "Path": "lastUtterance", "Value": ""},
    ]
    if account:
        patches.append({"Op": "SetField", "Path": "accountNumber", "Value": str(account)})

    return {
        "SheetName": sheet_name,
        "CarryInFromInstanceId": inp.get("CarryInFromInstanceId"),
        "Patches": patches,
    }
