from __future__ import annotations

import json
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]


def load_json(rel: str) -> dict[str, Any]:
    return json.loads((ROOT / rel).read_text())


GUARD = load_json("config/guardrails.json")
MISSIONS = load_json("config/catalogs/missions.json")
PUZZLES = load_json("config/catalogs/puzzles.json")
NPC = load_json("config/catalogs/npc.json")
ADAPT = load_json("config/catalogs/adaptation.json")


def clamp(value: float, lo: float, hi: float) -> float:
    return max(lo, min(hi, value))


def clamp_int(value: int, lo: int, hi: int) -> int:
    return int(clamp(value, lo, hi))


def confidence_label(raw: float | None) -> str:
    if raw is None:
        return "unknown"
    if raw >= 0.8:
        return "high"
    if raw >= 0.65:
        return "medium"
    if raw >= GUARD["room_scout"]["min_confidence"]:
        return "low"
    return "unknown"


def word_count(text: str) -> int:
    return len([w for w in text.replace("\n", " ").split(" ") if w])


def sanitize_line(text: str, fallback: str) -> str:
    cleaned = " ".join((text or "").split())
    for prefix in NPC["forbidden_prefixes"]:
        if cleaned.lower().startswith(prefix):
            return fallback
    if word_count(cleaned) == 0 or word_count(cleaned) > NPC["max_words"]:
        return fallback
    if any(ch in cleaned for ch in [";", "`", "{", "}"]):
        return fallback
    return cleaned


def template_by_id(template_id: str) -> dict[str, Any] | None:
    for t in MISSIONS["templates"]:
        if t["id"] == template_id:
            return t
    return None


def puzzle_by_id(puzzle_id: str) -> dict[str, Any] | None:
    for p in PUZZLES["puzzles"]:
        if p["id"] == puzzle_id:
            return p
    return None


def clamp_params(puzzle_type: str, params: dict[str, Any]) -> dict[str, int | float | str]:
    spec = puzzle_by_id(puzzle_type)
    if spec is None:
        return {}
    out: dict[str, int | float | str] = {}
    for key, bounds in spec.get("params", {}).items():
        raw = params.get(key, bounds["default"])
        try:
            num = float(raw)
        except (TypeError, ValueError):
            num = bounds["default"]
        num = clamp(num, bounds["min"], bounds["max"])
        out[key] = int(num) if float(num).is_integer() else num
    return out


PLAYER_ALLOW = set(GUARD["player_model"]["allowlist"])
PLAYER_FORBID = set(GUARD["player_model"]["forbid"])


def strip_player(data: dict[str, Any]) -> dict[str, Any]:
    return {k: v for k, v in data.items() if k in PLAYER_ALLOW and k not in PLAYER_FORBID}
