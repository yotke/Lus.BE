# -*- coding: utf-8 -*-
import json
import os
import subprocess
import sys

import pytest

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
GOLDEN = os.path.join(ROOT, "tests", "golden", "rst-hours-series.xlsx")
RUNNER = os.path.join(ROOT, "agents", "runner.py")


def _run(agent: str, draft=None, inp=None, lang="he"):
    payload = json.dumps({"Draft": draft or {}, "Input": inp or {}}, ensure_ascii=False)
    proc = subprocess.run(
        [sys.executable, RUNNER, "--agent", agent, "--lang", lang, "--non-interactive", "--payload-stdin"],
        input=payload,
        text=True,
        capture_output=True,
        cwd=ROOT,
        check=False,
    )
    assert proc.returncode == 0, proc.stderr
    return json.loads(proc.stdout.strip())


@pytest.fixture(scope="module")
def golden_path():
    assert os.path.isfile(GOLDEN), f"golden fixture missing: {GOLDEN}"
    return GOLDEN


def test_template_reader_identifies_five_blocks(golden_path):
    env = _run("doc.template_reader", inp={"FilePath": golden_path})
    assert env["Ok"] is True
    res = env["Result"]
    assert res["Rtl"] is True
    assert res["DataBandStartRow"] >= 11
    assert res["MergeCount"] >= 30
    blocks = res["FiveBlocks"]
    assert blocks["title"]
    assert blocks["letterhead"]
    assert blocks["tableHeader"]
    assert blocks["dataBandStart"]
    assert blocks["totals"]
    assert res["SheetName"] == "מרץ  2026 "


def test_formatter_remaining_hours():
    draft = {
        "Rows": [{"Hours": 4}, {"Hours": 4}],
        "Totals": {"CarryIn": 100, "VatPercent": 18},
    }
    env = _run("doc.formatter", draft=draft)
    assert env["Ok"] is True
    totals = env["Result"]["Totals"]
    assert totals["Hours"] == 8
    assert totals["Remaining"] == 92


def test_validator_empty_rate_fails():
    draft = {"Rows": [{"Hours": 1}], "Totals": {"HourlyRate": None}}
    env = _run("doc.validator", draft=draft)
    assert env["Ok"] is True
    assert env["Result"]["Ok"] is False
    codes = [w["Code"] for w in env["Result"]["Warnings"]]
    assert "empty_rate" in codes


def test_row_extractor_hebrew_line():
    env = _run("doc.row_extractor", inp={"Text": "5 במרץ 3 שעות במשרד — התייעצות"})
    assert env["Ok"] is True
    patches = env["Result"]["Patches"]
    assert len(patches) == 1
    row = patches[0]["Value"]
    assert row["Hours"] == 3
    assert row["Subject"]
    assert row["DayOfWeek"] == 3  # Thursday 2026-03-05


def test_carry_forward_clears_rows():
    draft = {"Rows": [{"Hours": 1}], "AccountNumber": "01032601"}
    env = _run("doc.carry_forward", draft=draft, inp={"Period": {"SheetName": "אפריל 2026"}})
    assert env["Ok"] is True
    ops = {p["Op"] for p in env["Result"]["Patches"]}
    assert "SetField" in ops
