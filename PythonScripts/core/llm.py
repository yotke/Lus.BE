"""One LLM client layer for all scripts (Phase 2).

Design (plan child 04 "cost-smart routing" + child 07 A-items):
- Strategy + Factory: providers are interchangeable; a per-TASK table picks one.
- Chain of Responsibility: candidate provider -> openai -> caller's deterministic
  fallback. A provider that is unavailable (lib not installed / no key) is
  skipped silently; a provider that ERRORS raises to the caller, preserving each
  script's existing error contract (golden-pinned).
- Env overrides: ARMYLUZ_LLM_PROVIDER__<task>=openai|gemini,
                 ARMYLUZ_LLM_MODEL__<task>=<pinned model id>.
- Default behavior with no env set is byte-identical to before: OpenAI,
  gpt-4o-mini — every task starts on tier T1/T2 defaults below.

Providers never appear in argv; Gemini config is env-only (core.env.gemini_config).
"""
from __future__ import annotations

import os
from dataclasses import dataclass

from .env import gemini_config

DEFAULT_MODEL = "gpt-4o-mini"

# Task routing table (T1 = golden-pinned primary; T2 = cheap-candidate tasks).
# Models are PINNED ids — never "-latest" aliases (plan child 04).
TASKS: dict[str, dict] = {
    # T1 — primary extraction, stays on OpenAI until a bake-off proves a swap
    "rules.extract":        {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
    "org.extract":          {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
    "org.converse":         {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.3},
    "day_edit.ops":         {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
    "shift.create":         {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
    "constraints.extract":  {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
    "availability.extract": {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
    "weights.tune":         {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
    # T2 — structured-but-simple tasks: Gemini flash-lite bake-off candidates
    "org.plan":             {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
    "org.observer":         {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
    "progress.enrich":      {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.7},
    "transliterate":        {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
    "subdomain":            {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.7},
    "analytics":            {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.3},
    "intent.classify":      {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0},
}


@dataclass
class TaskConfig:
    task: str
    provider: str
    model: str
    temperature: float


def resolve(task: str) -> TaskConfig:
    base = TASKS.get(task, {"provider": "openai", "model": DEFAULT_MODEL, "temperature": 0.0})
    provider = os.getenv(f"ARMYLUZ_LLM_PROVIDER__{task.replace('.', '_')}", base["provider"])
    model = os.getenv(f"ARMYLUZ_LLM_MODEL__{task.replace('.', '_')}", base["model"])
    return TaskConfig(task=task, provider=provider, model=model,
                      temperature=base.get("temperature", 0.0))


class LlmClient:
    """Minimal uniform surface: complete(system, user, json_mode=False) -> str."""

    def __init__(self, cfg: TaskConfig):
        self.cfg = cfg

    def complete(self, system: str, user: str, *, json_mode: bool = False) -> str:
        raise NotImplementedError


class _OpenAIClient(LlmClient):
    def complete(self, system: str, user: str, *, json_mode: bool = False) -> str:
        from openai import OpenAI  # lazy — envs without the SDK stay importable
        client = OpenAI()
        kwargs: dict = {}
        if json_mode:
            kwargs["response_format"] = {"type": "json_object"}
        resp = client.chat.completions.create(
            model=self.cfg.model,
            temperature=self.cfg.temperature,
            messages=[{"role": "system", "content": system},
                      {"role": "user", "content": user}],
            **kwargs,
        )
        return resp.choices[0].message.content or ""


class _GeminiClient(LlmClient):
    """Gemini via its OpenAI-compatible endpoint — no extra dependency.

    Uses the same `openai` SDK pointed at GEMINI_BASE_URL (env-only config).
    Unavailable (no key/base-url) -> get_llm skips this provider.
    """

    def __init__(self, cfg: TaskConfig, gcfg: dict):
        super().__init__(cfg)
        self._gcfg = gcfg

    def complete(self, system: str, user: str, *, json_mode: bool = False) -> str:
        from openai import OpenAI
        client = OpenAI(api_key=self._gcfg["api_key"],
                        base_url=self._gcfg["base_url"].rstrip("/") + "/openai/")
        kwargs: dict = {}
        if json_mode:
            kwargs["response_format"] = {"type": "json_object"}
        model = self._gcfg["model"] or self.cfg.model
        resp = client.chat.completions.create(
            model=model,
            temperature=self.cfg.temperature,
            messages=[{"role": "system", "content": system},
                      {"role": "user", "content": user}],
            **kwargs,
        )
        return resp.choices[0].message.content or ""


def get_llm(task: str) -> LlmClient:
    """Factory: resolve the task's provider chain; return the first AVAILABLE."""
    cfg = resolve(task)
    if cfg.provider == "gemini":
        gcfg = gemini_config()
        if gcfg["api_key"] and gcfg["base_url"]:
            return _GeminiClient(cfg, gcfg)
        # Provider unavailable -> fall through to openai (chain of responsibility).
    return _OpenAIClient(cfg)
