from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from contracts import PlayerProfile
from guardrails import strip_player


class MemoryStore:
    def __init__(self, root: Path) -> None:
        self.root = Path(root)
        self.players = self.root / "players"
        self.world = self.root / "world.json"
        self.last = self.root / "last_mission.json"
        self.sessions = self.root / "sessions"
        self.players.mkdir(parents=True, exist_ok=True)
        self.sessions.mkdir(parents=True, exist_ok=True)

    def load_player(self, player_id: str) -> PlayerProfile:
        path = self.players / f"{player_id}.json"
        if not path.exists():
            return PlayerProfile(id=player_id)
        data = strip_player(json.loads(path.read_text()))
        data.setdefault("id", player_id)
        extra = json.loads(path.read_text())
        profile = PlayerProfile(**data)
        profile.last_template_id = extra.get("last_template_id")
        profile.last_difficulty = extra.get("last_difficulty")
        return profile

    def save_player(self, player: PlayerProfile) -> None:
        payload = strip_player(player.model_dump())
        payload["last_template_id"] = player.last_template_id
        payload["last_difficulty"] = player.last_difficulty
        (self.players / f"{player.id}.json").write_text(json.dumps(payload, indent=2))

    def load_world(self) -> dict[str, Any]:
        if not self.world.exists():
            return {}
        return json.loads(self.world.read_text())

    def save_world(self, flags: dict[str, Any]) -> None:
        self.world.write_text(json.dumps(flags, indent=2))

    def save_last_mission(self, mission: dict[str, Any]) -> None:
        self.last.write_text(json.dumps(mission, indent=2))

    def save_session(self, session: dict[str, Any]) -> None:
        sid = session.get("id") or "session"
        (self.sessions / f"{sid}.json").write_text(json.dumps(session, indent=2))
