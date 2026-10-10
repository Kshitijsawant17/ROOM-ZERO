from __future__ import annotations

from typing import Any

from agents.base import Specialist
from contracts import PlayerProfile, SessionEvent
from guardrails import strip_player


class PlayerModel(Specialist[dict[str, Any], PlayerProfile]):
    def __init__(self) -> None:
        super().__init__("player_model", 200)

    def run(self, payload: dict[str, Any], ctx: dict[str, Any]) -> PlayerProfile:
        current = payload.get("profile") or PlayerProfile()
        if isinstance(current, dict):
            current = PlayerProfile(**strip_player(current))
        event = payload.get("event")
        if event is None:
            return current
        if isinstance(event, dict):
            event = SessionEvent(**event)
        sessions = current.sessions + 1
        wins = current.wins + int(event.won)
        fails = current.fails + int(event.failed)
        retries = current.retries + event.retries
        hints = current.hints + event.hints
        pace = round((current.pace * 0.7) + (event.actions_per_min * 0.3), 2)
        style = event.style or current.style
        if event.alarm_triggers >= 2:
            style = "chaos"
        elif event.actions_per_min > 28:
            style = "speed"
        favorite = event.favorite_surface or current.favorite_surface
        difficulty = _suggest(pace, fails, wins, hints)
        history = list(current.history[-8:])
        history.append(
            {
                "mission_id": event.mission_id,
                "won": event.won,
                "duration_s": event.duration_s,
                "style": style,
                "template_id": event.template_id,
            }
        )
        data = strip_player(
            {
                "id": current.id,
                "sessions": sessions,
                "wins": wins,
                "fails": fails,
                "pace": pace,
                "retries": retries,
                "hints": hints,
                "style": style,
                "favorite_surface": favorite,
                "suggested_difficulty": difficulty,
                "last_duration_s": event.duration_s,
                "history": history,
            }
        )
        profile = PlayerProfile(**data)
        profile.last_template_id = event.template_id
        profile.last_difficulty = event.difficulty
        return profile


def _suggest(pace: float, fails: int, wins: int, hints: int) -> str:
    if fails >= 2 or pace < 12 or hints >= 3:
        return "easy"
    if pace > 30 and fails == 0 and wins >= 1 and hints == 0:
        return "hard"
    return "normal"
