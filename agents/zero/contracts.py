from __future__ import annotations

from enum import Enum
from typing import Any, Literal

from pydantic import BaseModel, Field, field_validator

Difficulty = Literal["easy", "normal", "hard"]
Style = Literal["stealth", "speed", "chaos"]
SurfaceKind = Literal["desk", "table", "floor", "wall", "shelf", "door", "window", "chair", "stairs", "edge", "other"]
VolumeKind = Literal["open", "blocked", "playable"]
NpcIntent = Literal["SPEAK", "HINT", "WAIT", "ACKNOWLEDGE", "URGE", "FALLBACK"]
AdaptKind = Literal["HINT_BEAM", "EASE_TIMER", "SKIP_OPTIONAL", "LOWER_DIFFICULTY"]
Confidence = Literal["high", "medium", "low", "unknown"]


class SessionState(str, Enum):
    BOOT = "BOOT"
    ROOM_READY = "ROOM_READY"
    MISSION_REQUESTED = "MISSION_REQUESTED"
    PLANNING = "PLANNING"
    VALIDATING = "VALIDATING"
    BRIEFING = "BRIEFING"
    ACTIVE = "ACTIVE"
    ADAPTING = "ADAPTING"
    COMPLETE = "COMPLETE"
    SAVING = "SAVING"
    READY_FOR_NEXT_SESSION = "READY_FOR_NEXT_SESSION"


class Bounds(BaseModel):
    width_m: float = 0
    depth_m: float = 0
    height_m: float = 0


class RawSurface(BaseModel):
    id: str
    kind: str
    area_m2: float = 0
    height_m: float = 0
    anchor_id: str | None = None
    safe: bool | None = None
    confidence: float | None = None
    near_edge: bool = False
    blocked: bool = False


class RawVolume(BaseModel):
    id: str
    kind: str
    volume_m3: float = 0
    confidence: float | None = None


class RawHazard(BaseModel):
    id: str | None = None
    kind: str | None = None
    source: Literal["sensor", "user", "unknown"] = "unknown"
    confidence: float | None = None


class RoomSceneInput(BaseModel):
    room_id: str = "unknown"
    bounds: Bounds = Field(default_factory=Bounds)
    surfaces: list[RawSurface] = Field(default_factory=list)
    volumes: list[RawVolume] = Field(default_factory=list)
    hazards: list[RawHazard] = Field(default_factory=list)
    scanned_at: str | None = None


class AnchorRef(BaseModel):
    surface_id: str
    anchor_id: str
    kind: str
    area_m2: float
    confidence: Confidence


class RoomMap(BaseModel):
    room_id: str
    bounds: Bounds
    playable: list[AnchorRef] = Field(default_factory=list)
    excluded: list[dict[str, str]] = Field(default_factory=list)
    open_volume_m3: float = 0
    unknown_count: int = 0
    playable_area_m2: float = 0
    ready: bool = False


class PlayerProfile(BaseModel):
    id: str = "local"
    sessions: int = 0
    wins: int = 0
    fails: int = 0
    pace: float = 20
    retries: int = 0
    hints: int = 0
    style: Style = "stealth"
    favorite_surface: str | None = None
    suggested_difficulty: Difficulty = "normal"
    last_duration_s: float = 0
    last_template_id: str | None = None
    last_difficulty: Difficulty | None = None
    history: list[dict[str, Any]] = Field(default_factory=list)

    @field_validator("pace")
    @classmethod
    def clamp_pace(cls, v: float) -> float:
        return max(1.0, min(80.0, float(v)))


class MissionPlan(BaseModel):
    template_id: str
    theme: str
    title: str
    objective_ids: list[str]
    difficulty: Difficulty
    duration_s: int
    seed: int
    primary_anchor_id: str
    primary_kind: str


class PuzzleSpec(BaseModel):
    puzzle_id: str
    puzzle_type: str
    prefab: str
    target_anchor_id: str
    surface_id: str
    params: dict[str, int | float | str]
    requires: str | None = None


class DialogueLine(BaseModel):
    speaker: str
    intent: NpcIntent
    text: str
    state: str


class SessionEvent(BaseModel):
    kind: str
    mission_id: str | None = None
    duration_s: float = 0
    won: bool = False
    failed: bool = False
    retries: int = 0
    hints: int = 0
    actions_per_min: float = 20
    style: Style | None = None
    favorite_surface: str | None = None
    alarm_triggers: int = 0
    template_id: str | None = None
    difficulty: Difficulty | None = None


class PerformanceSignals(BaseModel):
    stuck_s: float = 0
    idle_s: float = 0
    puzzles_left: int = 1
    puzzles_solved: int = 0
    retries: int = 0
    alarm: float = 0
    last_adapt_s: float = 999
    adapts_used: int = 0
    current_difficulty: Difficulty = "normal"


class AdaptationProposal(BaseModel):
    kind: AdaptKind
    magnitude: float
    text: str
    safety_ok: bool = True


class ValidationReport(BaseModel):
    ok: bool
    errors: list[str] = Field(default_factory=list)
    plan: MissionPlan | None = None
    puzzles: list[PuzzleSpec] = Field(default_factory=list)
