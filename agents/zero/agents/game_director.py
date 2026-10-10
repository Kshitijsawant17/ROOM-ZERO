from __future__ import annotations

from typing import Any

from agents.base import Specialist
from contracts import MissionPlan, PlayerProfile, RoomMap
from guardrails import GUARD, MISSIONS, clamp_int

DIFF_RANK = GUARD["game_director"]["difficulty_rank"]
DUR = GUARD["game_director"]["duration_s"]


class GameDirector(Specialist[dict[str, Any], MissionPlan]):
    def __init__(self) -> None:
        super().__init__("game_director", 600)

    def run(self, payload: dict[str, Any], ctx: dict[str, Any]) -> MissionPlan:
        room: RoomMap = payload["room"]
        profile: PlayerProfile = payload["profile"]
        seed = int(payload.get("seed") or 2046)
        template = _choose_template(room, profile)
        if template is None:
            raise ValueError("no_supported_template")
        difficulty = _clamp_difficulty(profile, template)
        duration = clamp_int(template["duration_s"]["default"], DUR["min"], DUR["max"])
        duration = clamp_int(duration, template["duration_s"]["min"], template["duration_s"]["max"])
        primary = _primary_anchor(room, template, profile)
        if primary is None:
            raise ValueError("no_valid_anchor")
        return MissionPlan(
            template_id=template["id"],
            theme=template["theme"],
            title=template["title"],
            objective_ids=list(template["objective_ids"]),
            difficulty=difficulty,
            duration_s=duration,
            seed=seed,
            primary_anchor_id=primary.anchor_id,
            primary_kind=primary.kind,
        )


def _choose_template(room: RoomMap, profile: PlayerProfile) -> dict[str, Any] | None:
    kinds = {a.kind for a in room.playable}
    last = profile.last_template_id
    ranked = []
    for t in MISSIONS["templates"]:
        if not set(t["required_kinds"]) & kinds:
            continue
        if profile.suggested_difficulty not in t["difficulty"] and profile.suggested_difficulty == "hard":
            if "normal" not in t["difficulty"]:
                continue
        score = 0.0
        if profile.favorite_surface in t["required_kinds"]:
            score += 2
        if last and t["id"] == last:
            score -= 50
        else:
            score += 10
        score += max(a.area_m2 for a in room.playable if a.kind in t["required_kinds"])
        ranked.append((score, t))
    if not ranked:
        return None
    ranked.sort(key=lambda x: x[0], reverse=True)
    return ranked[0][1]


def _clamp_difficulty(profile: PlayerProfile, template: dict[str, Any]) -> str:
    wanted = profile.suggested_difficulty
    allowed = template["difficulty"]
    if wanted not in allowed:
        wanted = allowed[0]
    last = profile.last_difficulty
    if last and GUARD["game_director"]["difficulty_cooldown_sessions"] >= 1:
        jump = DIFF_RANK[wanted] - DIFF_RANK[last]
        if jump > 1:
            wanted = "normal" if last == "easy" else last
            if wanted not in allowed:
                wanted = allowed[0]
    if GUARD["game_director"]["never_make_safety_harder"] and wanted == "hard" and profile.fails >= 2:
        wanted = "normal" if "normal" in allowed else allowed[0]
    return wanted


def _primary_anchor(room: RoomMap, template: dict[str, Any], profile: PlayerProfile):
    needed = set(template["required_kinds"])
    cands = [a for a in room.playable if a.kind in needed]
    if not cands:
        return None
    if profile.favorite_surface:
        fav = [a for a in cands if a.kind == profile.favorite_surface]
        if fav:
            cands = fav
    return sorted(cands, key=lambda a: a.area_m2, reverse=True)[0]
