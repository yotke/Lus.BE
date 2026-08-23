# -*- coding: utf-8 -*-
"""Map table columns to semantic fields (deterministic default for archetype 1)."""

from __future__ import annotations


def run(*, draft, agent_input, lang="he"):
    return {
        "Columns": [
            {"Index": 0, "Letter": "A", "Name": "Date", "Type": "date"},
            {"Index": 1, "Letter": "B", "Name": "DayOfWeek", "Type": "derived"},
            {"Index": 2, "Letter": "C", "Name": "Hours", "Type": "number"},
            {"Index": 3, "Letter": "D", "Name": "Location", "Type": "text"},
            {"Index": 4, "Letter": "E", "Name": "Subject", "Type": "text"},
        ],
        "Notes": ["default_archetype1_mapping"],
    }
