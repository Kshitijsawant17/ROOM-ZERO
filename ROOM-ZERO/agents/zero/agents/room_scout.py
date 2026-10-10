from __future__ import annotations

from typing import Any

from agents.base import Specialist
from contracts import AnchorRef, RoomMap, RoomSceneInput
from guardrails import GUARD, confidence_label

EXCLUDED = set(GUARD["room_scout"]["exclude_kinds"])
EXCLUDED_VOL = set(GUARD["room_scout"]["exclude_volume_kinds"])
PLAYABLE_KINDS = {"desk", "table", "shelf", "floor", "wall", "window", "chair"}
MIN_CONF = GUARD["room_scout"]["min_confidence"]
MIN_AREA = GUARD["room_scout"]["min_area_m2"]


class RoomScout(Specialist[RoomSceneInput, RoomMap]):
    def __init__(self) -> None:
        super().__init__("room_scout", 400)

    def run(self, payload: RoomSceneInput, ctx: dict[str, Any]) -> RoomMap:
        playable: list[AnchorRef] = []
        excluded: list[dict[str, str]] = []
        unknown = 0
        for surface in payload.surfaces:
            kind = (surface.kind or "other").lower()
            conf = surface.confidence
            label = confidence_label(conf)
            if GUARD["room_scout"]["unknown_if_missing_confidence"] and conf is None:
                label = "unknown"
                unknown += 1
            reason = _exclude_reason(surface, kind, conf)
            if reason:
                excluded.append({"id": surface.id, "kind": kind, "reason": reason, "confidence": label})
                continue
            if not surface.anchor_id:
                excluded.append({"id": surface.id, "kind": kind, "reason": "no_anchor", "confidence": label})
                continue
            playable.append(
                AnchorRef(
                    surface_id=surface.id,
                    anchor_id=surface.anchor_id,
                    kind=kind,
                    area_m2=surface.area_m2,
                    confidence=label,  # type: ignore[arg-type]
                )
            )
        open_vol = 0.0
        for vol in payload.volumes:
            kind = (vol.kind or "").lower()
            if kind in EXCLUDED_VOL:
                excluded.append({"id": vol.id, "kind": kind, "reason": "blocked_volume", "confidence": confidence_label(vol.confidence)})
                continue
            if kind == "open":
                open_vol += vol.volume_m3
        for hazard in payload.hazards:
            source = hazard.source
            if source != "sensor" and GUARD["room_scout"]["never_infer_hazards"]:
                excluded.append({"id": hazard.id or "hazard", "kind": hazard.kind or "hazard", "reason": "unconfirmed_hazard", "confidence": "unknown"})
                continue
            excluded.append({"id": hazard.id or "hazard", "kind": hazard.kind or "hazard", "reason": "sensor_hazard", "confidence": confidence_label(hazard.confidence)})
        area = sum(a.area_m2 for a in playable)
        return RoomMap(
            room_id=payload.room_id,
            bounds=payload.bounds,
            playable=playable,
            excluded=excluded,
            open_volume_m3=open_vol,
            unknown_count=unknown,
            playable_area_m2=round(area, 3),
            ready=len(playable) >= 1 and area >= 0.3,
        )


def _exclude_reason(surface, kind: str, conf: float | None) -> str | None:
    if kind in EXCLUDED:
        return "excluded_kind"
    if kind not in PLAYABLE_KINDS:
        return "unsupported_kind"
    if surface.blocked:
        return "blocked"
    if surface.near_edge:
        return "edge"
    if surface.safe is False:
        return "marked_unsafe"
    if conf is not None and conf < MIN_CONF:
        return "low_confidence"
    if surface.area_m2 < MIN_AREA:
        return "too_small"
    return None
