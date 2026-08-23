# -*- coding: utf-8 -*-
"""Keyless echo agent — proves C# → stdin → Python → stdout Hebrew round-trip."""


def run(*, draft, agent_input, lang="he"):
    text = (agent_input or {}).get("Text") or (agent_input or {}).get("text") or ""
    return {"Echo": text, "Lang": lang}
