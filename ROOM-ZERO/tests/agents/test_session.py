from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "agents" / "zero"))

from contracts import SessionState
from session import ZeroSession


def _room():
    return json.loads((ROOT / "data" / "rooms" / "sample_bedroom.json").read_text())


def test_end_to_end_states(tmp_path):
    z = ZeroSession(tmp_path)
    snap = z.boot()
    assert snap["machine"]["state"] == "ROOM_READY"
    snap = z.handle("room_scan", {"room": _room()})
    assert snap["room"]["ready"]
    snap = z.handle("give_mission", {})
    assert snap["machine"]["state"] == "ACTIVE"
    assert snap["plan"]["template_id"]
    assert snap["puzzles"]
    assert all(p["target_anchor_id"] for p in snap["puzzles"])
    agents = [i["agent"] for i in snap["invokes"]]
    assert agents.count("room_scout") == 1
    assert "game_director" in agents
    assert "puzzle_architect" in agents
    snap = z.handle("live", {"stuck_s": 30, "idle_s": 0, "puzzles_left": 2, "last_adapt_s": 40})
    assert snap["machine"]["state"] == "ACTIVE"
    assert snap["adapt"]["kind"] in {"HINT_BEAM", "SKIP_OPTIONAL", "EASE_TIMER", "LOWER_DIFFICULTY"}
    snap = z.handle("complete", {"won": True, "duration_s": 90, "actions_per_min": 24})
    assert snap["machine"]["state"] == "READY_FOR_NEXT_SESSION"
    assert snap["profile"]["sessions"] == 1
    assert snap["profile"]["wins"] == 1


def test_cannot_invoke_wrong_specialist(tmp_path):
    z = ZeroSession(tmp_path)
    z.boot()
    try:
        z.invoke("game_director", {})
        raised = False
    except RuntimeError as exc:
        raised = "specialist_not_allowed" in str(exc)
    assert raised


def test_second_session_avoids_last_template(tmp_path):
    z = ZeroSession(tmp_path)
    z.boot()
    z.handle("room_scan", {"room": _room()})
    first = z.handle("give_mission", {})
    tid = first["plan"]["template_id"]
    z.handle("complete", {"won": True, "duration_s": 80, "actions_per_min": 22})
    second = z.handle("give_mission", {})
    assert second["machine"]["state"] == "ACTIVE"
    if len({a["kind"] for a in second["room"]["playable"]}) > 1:
        assert second["plan"]["template_id"] != tid or second["plan"]["primary_kind"] != first["plan"]["primary_kind"]
