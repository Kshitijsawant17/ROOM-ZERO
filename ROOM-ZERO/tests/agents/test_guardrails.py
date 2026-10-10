from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "agents" / "zero"))

from agents.adaptation import AdaptationAgent
from agents.game_director import GameDirector
from agents.npc_agent import NpcAgent
from agents.puzzle_architect import PuzzleArchitect
from agents.room_scout import RoomScout
from contracts import (
    AdaptationProposal,
    PerformanceSignals,
    PlayerProfile,
    PuzzleSpec,
    RawHazard,
    RawSurface,
    RoomMap,
    RoomSceneInput,
)
from guardrails import sanitize_line, strip_player
from validator import validate


def _scene() -> RoomSceneInput:
    return RoomSceneInput(
        room_id="lab",
        surfaces=[
            RawSurface(id="desk_a", kind="desk", area_m2=1.2, anchor_id="a1", confidence=0.9, safe=True),
            RawSurface(id="table_a", kind="table", area_m2=1.8, anchor_id="a2", confidence=0.85, safe=True),
            RawSurface(id="shelf_a", kind="shelf", area_m2=0.4, anchor_id="a3", confidence=0.8, safe=True),
            RawSurface(id="door_a", kind="door", area_m2=1.8, anchor_id="a4", confidence=0.9, safe=True),
            RawSurface(id="stair_a", kind="stairs", area_m2=2.0, anchor_id="a5", confidence=0.9, safe=True),
            RawSurface(id="edge_a", kind="desk", area_m2=1.0, anchor_id="a6", confidence=0.9, near_edge=True),
            RawSurface(id="unk_a", kind="desk", area_m2=1.0, anchor_id="a7", confidence=None),
        ],
        hazards=[RawHazard(id="maybe", kind="glass", source="unknown")],
    )


def test_scout_excludes_doors_stairs_edges_and_unconfirmed_hazards():
    room = RoomScout().run(_scene(), {})
    kinds = {a.kind for a in room.playable}
    ids = {a.surface_id for a in room.playable}
    assert "door" not in kinds
    assert "stairs" not in kinds
    assert "door_a" not in ids
    assert "stair_a" not in ids
    assert "edge_a" not in ids
    reasons = {e["reason"] for e in room.excluded}
    assert "unconfirmed_hazard" in reasons
    assert room.unknown_count >= 1


def test_director_catalog_only_and_clamped():
    room = RoomScout().run(_scene(), {})
    profile = PlayerProfile(suggested_difficulty="hard", last_difficulty="easy", fails=0)
    plan = GameDirector().run({"room": room, "profile": profile, "seed": 9}, {})
    assert 120 <= plan.duration_s <= 240
    assert plan.template_id in {"desk_vault", "shelf_wire", "table_map", "wall_circuit", "window_extract"}
    assert plan.difficulty in {"easy", "normal", "hard"}
    assert plan.difficulty != "hard"


def test_architect_anchor_ids_only():
    room = RoomScout().run(_scene(), {})
    plan = GameDirector().run({"room": room, "profile": PlayerProfile(), "seed": 3}, {})
    specs = PuzzleArchitect().run({"plan": plan, "room": room}, {})
    assert specs
    assert all(s.target_anchor_id for s in specs)
    assert all("x" not in s.params and "y" not in s.params for s in specs)
    report = validate(plan, specs, room)
    assert report.ok, report.errors


def test_architect_rejects_coordinates_via_validator():
    room = RoomScout().run(_scene(), {})
    plan = GameDirector().run({"room": room, "profile": PlayerProfile(), "seed": 3}, {})
    specs = PuzzleArchitect().run({"plan": plan, "room": room}, {})
    bad = specs[0].model_copy(update={"params": {**specs[0].params, "x": 1}})
    report = validate(plan, [bad], room)
    assert not report.ok
    assert any("coordinates" in e or "unbounded" in e for e in report.errors)


def test_npc_intent_and_word_limit():
    line = NpcAgent().run({"persona": "zero", "intent": "HACK", "state": "BRIEFING"}, {})
    assert line.intent == "FALLBACK" or line.intent == "SPEAK"
    assert len(line.text.split()) <= 25
    assert sanitize_line("exec rm -rf /", "Room is live. Stay quiet.") == "Room is live. Stay quiet."


def test_player_model_strips_personal_data():
    stripped = strip_player({"id": "local", "email": "a@b.c", "pace": 20, "name": "Ada", "wins": 1})
    assert "email" not in stripped
    assert "name" not in stripped
    assert "wins" in stripped


def test_adaptation_one_bounded_and_not_harder():
    agent = AdaptationAgent()
    out = agent.run(PerformanceSignals(stuck_s=30, puzzles_left=1, last_adapt_s=40, adapts_used=0), {})
    assert isinstance(out, AdaptationProposal)
    assert out.kind in {"HINT_BEAM", "EASE_TIMER", "SKIP_OPTIONAL", "LOWER_DIFFICULTY"}
    assert out.safety_ok
    denied = agent.run(PerformanceSignals(stuck_s=30, last_adapt_s=1, adapts_used=0), {})
    assert denied is None
    capped = agent.run(PerformanceSignals(stuck_s=30, last_adapt_s=40, adapts_used=2), {})
    assert capped is None
