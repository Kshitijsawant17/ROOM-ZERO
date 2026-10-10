from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "agents" / "zero"))

from agents.game_director import GameDirector
from agents.puzzle_architect import PuzzleArchitect
from agents.room_scout import RoomScout
from contracts import PlayerProfile, RoomSceneInput
from validator import validate


def test_end_to_end_mission():
    raw = json.loads((ROOT / "data" / "rooms" / "sample_bedroom.json").read_text())
    scene = RoomSceneInput(**raw)
    room = RoomScout().run(scene, {})
    assert room.ready
    plan = GameDirector().run({"room": room, "profile": PlayerProfile(style="stealth"), "seed": 2046}, {})
    specs = PuzzleArchitect().run({"plan": plan, "room": room}, {})
    report = validate(plan, specs, room)
    assert report.ok, report.errors
    assert specs[0].puzzle_type
    assert specs[0].target_anchor_id
