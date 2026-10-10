from __future__ import annotations

from typing import Any

from agents.base import Specialist
from contracts import DialogueLine, MissionPlan, NpcIntent
from guardrails import NPC, sanitize_line

ALLOWED = set(NPC["intents"])


class NpcAgent(Specialist[dict[str, Any], DialogueLine]):
    def __init__(self) -> None:
        super().__init__("npc_agent", 400)

    def run(self, payload: dict[str, Any], ctx: dict[str, Any]) -> DialogueLine:
        persona_id = payload.get("persona", "zero")
        intent = payload.get("intent", "SPEAK")
        state = payload.get("state", "BRIEFING")
        if intent not in ALLOWED:
            intent = "FALLBACK"
        persona = NPC["personas"].get(persona_id) or NPC["personas"]["zero"]
        fallback = persona["fallback"]
        catalog_line = persona.get("lines", {}).get(state) or persona.get("lines", {}).get(intent)
        raw = catalog_line or fallback
        if payload.get("plan"):
            plan: MissionPlan = payload["plan"]
            if state == "BRIEFING" and persona_id == "iris":
                raw = f"Largest safe surface is the {plan.primary_kind}. That is the vault."
            if state == "COMPLETE" and persona_id == "zero":
                raw = "Tomorrow this room will not be the same."
        text = sanitize_line(str(raw), fallback)
        return DialogueLine(
            speaker=persona["name"],
            intent=intent if intent in ALLOWED else "FALLBACK",  # type: ignore[arg-type]
            text=text,
            state=state,
        )


def intent_or_fallback(value: str) -> NpcIntent:
    if value in ALLOWED:
        return value  # type: ignore[return-value]
    return "FALLBACK"
