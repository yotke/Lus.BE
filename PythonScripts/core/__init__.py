"""core — the shared kernel of PythonScripts.

One implementation each of: env/API-key handling (`core.env`), LLM client
construction (`core.llm`, multi-provider), tolerant JSON parsing + stdout
emission (`core.jsonio`), the result envelope (`core.result`), and the stderr
protocol the C# adapter parses (`core.logging`).

Import direction (enforced by tests/test_module_boundaries.py):
    engines -> engines/shared -> core        (never the reverse)
    core imports NO other project package.
"""
