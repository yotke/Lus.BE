# PythonScripts — Lus agent runtime

C# `PythonScriptsAdapter.RunAgentAsync` (P1) spawns `python agents/runner.py --agent doc.<name> --payload-stdin`.

This folder is **not** a copy of ArmyLuz `agents/org` or `agents/rules`. Lus agents live under `agents/doc/`. P0 only installs the interpreter and `openpyxl`; the runner and `doc.echo` land in P1.

See `docs/PYTHON_AGENTS_BRIDGE.md`.
