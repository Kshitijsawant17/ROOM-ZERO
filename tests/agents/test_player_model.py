from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "agents" / "zero"))

from agents.player_model import PlayerModel
from contracts import PlayerProfile, SessionEvent


def test_session_updates_wins_and_history():
    current = PlayerProfile(id="local", sessions=0, wins=0, fails=0, pace=32, style="stealth")
    event = SessionEvent(
        kind="session_end",
        won=True,
        failed=False,
        actions_per_min=40,
        duration_s=90,
        mission_id="t1",
        template_id="desk_vault",
        difficulty="normal",
    )
    out = PlayerModel().run({"profile": current, "event": event}, {})
    assert out.wins == 1
    assert out.suggested_difficulty == "hard"
    assert out.history[-1]["mission_id"] == "t1"
    assert "email" not in out.model_dump()
