# -*- coding: utf-8 -*-
"""Importer: exemplar .xlsx → DocumentTemplate skeleton (no LLM)."""

from __future__ import annotations

import os

from openpyxl import load_workbook

from agents.doc._workbook_scan import (
    cell_text,
    column_widths,
    find_billing_start,
    find_declaration_start,
    find_table_header_row,
    find_totals_row,
    merged_range_count,
    pick_month_sheet,
    scan_letterhead,
    sheet_names,
    title_row,
)


def _resolve_path(agent_input: dict) -> str | None:
    if not agent_input:
        return None
    for key in ("FilePath", "filePath", "Path", "path"):
        if agent_input.get(key):
            return str(agent_input[key])
    return None


def run(*, draft, agent_input, lang="he"):
    path = _resolve_path(agent_input)
    if not path or not os.path.isfile(path):
        raise FileNotFoundError(f"Exemplar file not found: {path!r}")

    wb = load_workbook(path, data_only=False, read_only=False)
    try:
        names = sheet_names(wb)
        sheet_name = pick_month_sheet(names)
        if not sheet_name:
            raise ValueError("No month sheet found in workbook")

        ws = wb[sheet_name]
        header_row = find_table_header_row(ws)
        if header_row is None:
            raise ValueError("Table header row not found")

        data_start = header_row + 1
        totals_row = find_totals_row(ws, data_start) or (data_start + 14)
        billing_start = find_billing_start(ws, totals_row) or (totals_row + 3)
        declaration_start = find_declaration_start(ws, billing_start) or (billing_start + 6)
        t_row = title_row(ws) or 4
        letterhead = scan_letterhead(ws)
        merge_count = merged_range_count(ws)
        rtl = bool(getattr(ws.sheet_view, "rightToLeft", False))

        # Column headings exactly as the workbook writes them — the canvas shows the
        # user's own labels rather than ours.
        headers = []
        for c in range(1, ws.max_column + 1):
            label = cell_text(ws.cell(header_row, c).value)
            if label:
                headers.append(label)

        title_text = cell_text(ws.cell(t_row, 1).value)
        org_name = ""
        for r in range(1, t_row):
            candidate = cell_text(ws.cell(r, 1).value)
            if candidate:
                org_name = candidate
                break

        # The billing block's own wording (rate / subtotal / VAT / total), so the preview
        # reproduces the document's vocabulary instead of inventing labels.
        billing_labels = []
        for r in range(billing_start, declaration_start):
            label = cell_text(ws.cell(r, 1).value)
            if label:
                billing_labels.append(label)

        declaration_text = cell_text(ws.cell(declaration_start, 1).value)

        five_blocks = {
            "title": t_row,
            "letterhead": max(5, t_row + 1),
            "tableHeader": header_row,
            "dataBandStart": data_start,
            "totals": totals_row,
            "billing": billing_start,
            "declaration": declaration_start,
        }

        return {
            "SheetName": sheet_name,
            "Rtl": rtl,
            "ColumnWidths": column_widths(ws),
            "MergeCount": merge_count,
            "MergePolicy": "group-date-columns-AB",
            "DataBandStartRow": data_start,
            "TableHeaderRow": header_row,
            "TitleRow": t_row,
            "TotalsRow": totals_row,
            "BillingStartRow": billing_start,
            "DeclarationStartRow": declaration_start,
            "Letterhead": letterhead,
            "FiveBlocks": five_blocks,
            # Everything the canvas needs to PREVIEW the finished document, not just to
            # know where the data band starts: the title band, the letterhead lines, the
            # real column headings and the billing block's own labels. Geometry alone
            # renders a bare grid; these are what make it look like the workbook.
            "Patches": [
                {"Op": "SetField", "Path": "template.rtl", "Value": rtl},
                {"Op": "SetField", "Path": "template.dataBandStartRow", "Value": data_start},
                {"Op": "SetField", "Path": "template.mergePolicy", "Value": "group-date-columns-AB"},
                {"Op": "SetField", "Path": "template.sheetName", "Value": sheet_name},
                {"Op": "SetField", "Path": "template.tableHeaderRow", "Value": header_row},
                {"Op": "SetField", "Path": "template.titleRow", "Value": t_row},
                {"Op": "SetField", "Path": "template.totalsRow", "Value": totals_row},
                {"Op": "SetField", "Path": "template.billingStartRow", "Value": billing_start},
                {"Op": "SetField", "Path": "template.declarationStartRow", "Value": declaration_start},
                {"Op": "SetField", "Path": "template.mergeCount", "Value": merge_count},
                {"Op": "SetField", "Path": "template.columnWidths", "Value": column_widths(ws)},
                {"Op": "SetField", "Path": "template.headers", "Value": headers},
                {"Op": "SetField", "Path": "template.title", "Value": title_text},
                {"Op": "SetField", "Path": "template.orgName", "Value": org_name},
                {"Op": "SetField", "Path": "template.plannerName", "Value": letterhead.get("planner")},
                {"Op": "SetField", "Path": "template.clientName", "Value": letterhead.get("client")},
                {"Op": "SetField", "Path": "template.billingLabels", "Value": billing_labels},
                {"Op": "SetField", "Path": "template.declarationText", "Value": declaration_text},
            ]
            + (
                [{"Op": "SetField", "Path": "accountNumber", "Value": letterhead["account_number"]}]
                if letterhead.get("account_number")
                else []
            ),
        }
    finally:
        wb.close()
