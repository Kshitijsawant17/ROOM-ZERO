from __future__ import annotations

from contracts import MissionPlan, PuzzleSpec, RoomMap, ValidationReport
from guardrails import GUARD, MISSIONS, PUZZLES, puzzle_by_id, template_by_id


def validate(plan: MissionPlan, puzzles: list[PuzzleSpec], room: RoomMap) -> ValidationReport:
    errors: list[str] = []
    template = template_by_id(plan.template_id)
    if template is None:
        errors.append("template_not_in_catalog")
    else:
        if plan.theme not in MISSIONS["themes"]:
            errors.append("theme_not_in_catalog")
        for oid in plan.objective_ids:
            if oid not in MISSIONS["objective_types"]:
                errors.append(f"objective_not_in_catalog:{oid}")
        if plan.difficulty not in template["difficulty"]:
            errors.append("difficulty_not_allowed")
        if plan.duration_s < template["duration_s"]["min"] or plan.duration_s > template["duration_s"]["max"]:
            errors.append("duration_out_of_range")
    anchors = {a.anchor_id: a for a in room.playable}
    if plan.primary_anchor_id not in anchors:
        errors.append("primary_anchor_not_playable")
    seen = set()
    if len(puzzles) > GUARD["puzzle_architect"]["max_objects"]:
        errors.append("too_many_objects")
    for spec in puzzles:
        cat = puzzle_by_id(spec.puzzle_type)
        if cat is None:
            errors.append(f"puzzle_not_in_catalog:{spec.puzzle_type}")
            continue
        if spec.prefab != cat["prefab"]:
            errors.append(f"prefab_mismatch:{spec.puzzle_id}")
        if spec.target_anchor_id not in anchors:
            errors.append(f"anchor_not_playable:{spec.target_anchor_id}")
            continue
        surface = anchors[spec.target_anchor_id]
        if surface.kind not in cat["surfaces"]:
            errors.append(f"surface_not_supported:{spec.puzzle_id}")
        if spec.target_anchor_id in seen:
            errors.append(f"duplicate_anchor:{spec.target_anchor_id}")
        seen.add(spec.target_anchor_id)
        for key in spec.params:
            if key not in cat.get("params", {}):
                errors.append(f"unbounded_param:{key}")
        if any(k in spec.params for k in ("x", "y", "z", "position", "xyz")):
            errors.append("coordinates_forbidden")
    limit = PUZZLES["max_per_mission"].get(plan.difficulty, 3)
    if len(puzzles) > limit:
        errors.append("over_difficulty_limit")
    return ValidationReport(ok=not errors, errors=errors, plan=plan, puzzles=puzzles)
