from __future__ import annotations

from typing import Any

from agents.base import Specialist
from contracts import AdaptationProposal, PerformanceSignals
from guardrails import ADAPT, clamp

ALLOWED = set(ADAPT["kinds"])
FORBIDDEN = set(ADAPT["forbidden_kinds"])


class AdaptationAgent(Specialist[PerformanceSignals, AdaptationProposal | None]):
    def __init__(self) -> None:
        super().__init__("adaptation", 200)

    def run(self, payload: PerformanceSignals, ctx: dict[str, Any]) -> AdaptationProposal | None:
        if payload.adapts_used >= ADAPT["max_per_mission"]:
            return None
        if payload.last_adapt_s < ADAPT["cooldown_s"]:
            return None
        kind = _choose(payload)
        if kind is None or kind in FORBIDDEN or kind not in ALLOWED:
            return None
        mag = ADAPT["magnitudes"][kind]
        magnitude = clamp(mag["default"], mag["min"], mag["max"])
        if kind == "LOWER_DIFFICULTY" and payload.current_difficulty == "easy":
            return None
        text = {
            "HINT_BEAM": "ZERO: I moved the vent. Go.",
            "EASE_TIMER": "Iris: Extra time. Vault still counts.",
            "SKIP_OPTIONAL": "Iris: Skip the extra node. Vault still counts.",
            "LOWER_DIFFICULTY": "ZERO: I eased the lock. Stay in the room.",
        }[kind]
        return AdaptationProposal(kind=kind, magnitude=float(magnitude), text=text, safety_ok=True)


def _choose(s: PerformanceSignals) -> str | None:
    if s.stuck_s >= ADAPT["stuck_s"] and s.puzzles_left > 1:
        return "SKIP_OPTIONAL"
    if s.stuck_s >= ADAPT["stuck_s"]:
        return "HINT_BEAM"
    if s.retries >= 3 and s.current_difficulty != "easy":
        return "LOWER_DIFFICULTY"
    if s.idle_s >= 40:
        return "EASE_TIMER"
    return None
