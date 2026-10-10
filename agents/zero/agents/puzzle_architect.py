from __future__ import annotations

from typing import Any

from agents.base import Specialist
from contracts import MissionPlan, PuzzleSpec, RoomMap
from guardrails import GUARD, PUZZLES, clamp_params, puzzle_by_id, template_by_id


class PuzzleArchitect(Specialist[dict[str, Any], list[PuzzleSpec]]):
    def __init__(self) -> None:
        super().__init__("puzzle_architect", 600)

    def run(self, payload: dict[str, Any], ctx: dict[str, Any]) -> list[PuzzleSpec]:
        plan: MissionPlan = payload["plan"]
        room: RoomMap = payload["room"]
        template = template_by_id(plan.template_id)
        if template is None:
            raise ValueError("unknown_template")
        limit = min(PUZZLES["max_per_mission"][plan.difficulty], GUARD["puzzle_architect"]["max_objects"])
        used_anchors: set[str] = set()
        specs: list[PuzzleSpec] = []
        puzzle_ids = list(template["puzzle_ids"])[:limit]
        for i, pid in enumerate(puzzle_ids):
            catalog = puzzle_by_id(pid)
            if catalog is None:
                continue
            anchor = _pick_anchor(room, catalog["surfaces"], used_anchors, prefer=plan.primary_anchor_id if i == 0 else None)
            if anchor is None:
                continue
            if GUARD["puzzle_architect"]["require_anchor_id"] and not anchor.anchor_id:
                continue
            used_anchors.add(anchor.anchor_id)
            params = clamp_params(pid, {"seed": plan.seed})
            spec = PuzzleSpec(
                puzzle_id=f"{pid}_{anchor.surface_id}",
                puzzle_type=pid,
                prefab=catalog["prefab"],
                target_anchor_id=anchor.anchor_id,
                surface_id=anchor.surface_id,
                params=params,
                requires=specs[-1].puzzle_id if specs else None,
            )
            specs.append(spec)
            if len(specs) >= limit:
                break
        if not specs:
            raise ValueError("no_legal_puzzles")
        if any("x" in s.params or "y" in s.params or "z" in s.params for s in specs):
            raise ValueError("coordinates_forbidden")
        return specs


def _pick_anchor(room: RoomMap, kinds: list[str], used: set[str], prefer: str | None):
    if prefer:
        for a in room.playable:
            if a.anchor_id == prefer and a.kind in kinds and a.anchor_id not in used:
                return a
    cands = [a for a in room.playable if a.kind in kinds and a.anchor_id not in used]
    if not cands:
        return None
    return sorted(cands, key=lambda a: a.area_m2, reverse=True)[0]
