from __future__ import annotations

import time
from concurrent.futures import ThreadPoolExecutor, TimeoutError as FutTimeout
from pathlib import Path
from typing import Any
from uuid import uuid4

from agents.adaptation import AdaptationAgent
from agents.game_director import GameDirector
from agents.npc_agent import NpcAgent
from agents.player_model import PlayerModel
from agents.puzzle_architect import PuzzleArchitect
from agents.room_scout import RoomScout
from contracts import (
    AdaptationProposal,
    DialogueLine,
    MissionPlan,
    PerformanceSignals,
    PlayerProfile,
    RoomMap,
    RoomSceneInput,
    SessionEvent,
    SessionState,
)
from memory.store import MemoryStore
from state_machine import StateMachine
from validator import validate


class ZeroSession:
    def __init__(self, memory_dir: Path, pool: ThreadPoolExecutor | None = None) -> None:
        self.store = MemoryStore(memory_dir)
        self.machine = StateMachine()
        self.pool = pool or ThreadPoolExecutor(max_workers=1, thread_name_prefix="zero")
        self.room_scout = RoomScout()
        self.game_director = GameDirector()
        self.puzzle_architect = PuzzleArchitect()
        self.npc_agent = NpcAgent()
        self.player_model = PlayerModel()
        self.adaptation_agent = AdaptationAgent()
        self.specialists = {
            "room_scout": self.room_scout,
            "game_director": self.game_director,
            "puzzle_architect": self.puzzle_architect,
            "npc_agent": self.npc_agent,
            "player_model": self.player_model,
            "adaptation": self.adaptation_agent,
        }
        self.invokes: list[dict[str, Any]] = []
        self.world: dict[str, Any] = self._empty_world()

    def _empty_world(self) -> dict[str, Any]:
        return {
            "profile": self.store.load_player("local"),
            "room": None,
            "plan": None,
            "puzzles": [],
            "briefing": [],
            "last_adapt": None,
            "adapts_used": 0,
            "outcome": None,
            "last_line": None,
            "errors": [],
        }

    def invoke(self, agent_id: str, payload: Any) -> Any:
        expected = self.machine.specialist()
        if expected is None or agent_id != expected:
            raise RuntimeError(
                f"specialist_not_allowed:{agent_id}_in_{self.machine.state.value}_expected_{expected}"
            )
        agent = self.specialists[agent_id]
        started = time.monotonic()
        fut = self.pool.submit(agent.run, payload, self.world)
        try:
            result = fut.result(timeout=agent.timeout_ms / 1000.0)
            status = "ok"
        except FutTimeout:
            result = None
            status = "timeout"
        except Exception as exc:
            result = None
            status = f"error:{exc}"
        self.invokes.append(
            {
                "agent": agent_id,
                "state": self.machine.state.value,
                "ms": round((time.monotonic() - started) * 1000, 2),
                "status": status,
            }
        )
        return result

    def snapshot(self, extra: dict[str, Any] | None = None) -> dict[str, Any]:
        body = {
            "machine": self.machine.snapshot(),
            "room": self.world["room"].model_dump() if self.world["room"] else None,
            "plan": self.world["plan"].model_dump() if self.world["plan"] else None,
            "puzzles": [p.model_dump() for p in self.world["puzzles"]],
            "briefing": [d.model_dump() for d in self.world["briefing"]],
            "profile": self.world["profile"].model_dump(),
            "adapt": self.world["last_adapt"].model_dump() if self.world["last_adapt"] else None,
            "outcome": self.world["outcome"],
            "line": self.world["last_line"].model_dump() if self.world["last_line"] else None,
            "invokes": list(self.invokes[-12:]),
            "errors": list(self.world["errors"][-8:]),
        }
        if extra:
            body.update(extra)
        return body

    def boot(self) -> dict[str, Any]:
        self.machine = StateMachine()
        self.invokes.clear()
        self.world = self._empty_world()
        self.machine.go(SessionState.ROOM_READY, reason="boot")
        return self.snapshot()

    def handle(self, kind: str, payload: dict[str, Any] | None = None) -> dict[str, Any]:
        payload = payload or {}
        if self.machine.timed_out():
            self.machine.on_timeout()
        if kind == "boot":
            return self.boot()
        if kind == "room_scan":
            return self.room_scan(payload)
        if kind in {"voice", "give_mission", "mission_requested"}:
            return self.mission_requested(payload)
        if kind == "live":
            return self.live(payload)
        if kind in {"complete", "session_end"}:
            return self.complete(payload)
        if kind == "timeout":
            self.machine.on_timeout()
            return self.snapshot()
        return self.snapshot({"error": "unknown_event"})

    def room_scan(self, payload: dict[str, Any]) -> dict[str, Any]:
        if self.machine.state == SessionState.BOOT:
            self.machine.go(SessionState.ROOM_READY, reason="scan")
        if self.machine.state == SessionState.READY_FOR_NEXT_SESSION:
            self.machine.go(SessionState.ROOM_READY, reason="rescan")
        if self.machine.state != SessionState.ROOM_READY:
            return self.snapshot({"error": "scan_not_allowed"})
        scene = RoomSceneInput(**(payload.get("room") or payload))
        result = self.invoke("room_scout", scene)
        if result is None:
            self.world["errors"].append("room_scout_timeout")
            self.machine.on_error("room_scout_timeout")
            return self.snapshot({"error": "room_scout_timeout"})
        self.world["room"] = result
        if not result.ready:
            self.world["errors"].append("room_not_ready")
            self.machine.on_error("room_not_ready")
            return self.snapshot({"error": "room_not_ready"})
        return self.snapshot()

    def mission_requested(self, payload: dict[str, Any]) -> dict[str, Any]:
        if self.machine.state == SessionState.ACTIVE:
            return self.snapshot({"error": "mission_not_allowed"})
        if self.machine.state == SessionState.READY_FOR_NEXT_SESSION:
            self.machine.go(SessionState.MISSION_REQUESTED, reason="player_request")
        elif self.machine.state == SessionState.ROOM_READY:
            self.machine.go(SessionState.MISSION_REQUESTED, reason="player_request")
        else:
            return self.snapshot({"error": "mission_not_allowed"})
        profile = self.invoke("player_model", {"profile": self.world["profile"]})
        if profile is None:
            self.machine.on_error("player_model_timeout")
            return self.snapshot({"error": "player_model_timeout"})
        self.world["profile"] = profile
        self.machine.go(SessionState.PLANNING, reason="profile_ready")
        return self._plan()

    def _plan(self) -> dict[str, Any]:
        room: RoomMap | None = self.world["room"]
        if room is None:
            self.machine.on_error("no_room")
            return self.snapshot({"error": "no_room"})
        seed = int(uuid4().hex[:8], 16) % 10_000_000
        plan = self.invoke("game_director", {"room": room, "profile": self.world["profile"], "seed": seed})
        if plan is None:
            self.machine.on_error("director_failed")
            return self.snapshot({"error": "director_failed"})
        self.world["plan"] = plan
        self.machine.go(SessionState.VALIDATING, reason="plan_ready")
        return self._validate()

    def _validate(self) -> dict[str, Any]:
        puzzles = self.invoke("puzzle_architect", {"plan": self.world["plan"], "room": self.world["room"]})
        if puzzles is None:
            self.machine.on_error("architect_failed")
            return self.snapshot({"error": "architect_failed"})
        report = validate(self.world["plan"], puzzles, self.world["room"])
        if not report.ok:
            self.world["errors"].append(";".join(report.errors))
            self.machine.on_error("validation_failed")
            return self.snapshot({"error": "validation_failed", "errors": report.errors})
        self.world["puzzles"] = puzzles
        self.machine.go(SessionState.BRIEFING, reason="validated")
        return self._brief()

    def _brief(self) -> dict[str, Any]:
        lines: list[DialogueLine] = []
        for persona in ("zero", "iris"):
            line = self.invoke("npc_agent", {"persona": persona, "intent": "SPEAK", "state": "BRIEFING", "plan": self.world["plan"]})
            if line:
                lines.append(line)
        self.world["briefing"] = lines
        self.world["adapts_used"] = 0
        self.world["last_adapt"] = None
        self.machine.go(SessionState.ACTIVE, reason="briefed")
        return self.snapshot()

    def live(self, payload: dict[str, Any]) -> dict[str, Any]:
        if self.machine.state == SessionState.ADAPTING:
            self.machine.go(SessionState.ACTIVE, reason="adapt_done")
        if self.machine.state != SessionState.ACTIVE:
            return self.snapshot({"error": "live_not_active"})
        signals = PerformanceSignals(**payload)
        signals.adapts_used = self.world["adapts_used"]
        if self.world["plan"]:
            signals.current_difficulty = self.world["plan"].difficulty
        needs = signals.stuck_s >= 25 or signals.idle_s >= 40 or signals.retries >= 3
        if not needs:
            return self.snapshot()
        self.machine.go(SessionState.ADAPTING, reason="performance")
        proposal = self.invoke("adaptation", signals)
        if not isinstance(proposal, AdaptationProposal) or not proposal.safety_ok:
            self.machine.go(SessionState.ACTIVE, reason="no_adapt")
            self.world["last_adapt"] = None
            return self.snapshot()
        self.world["last_adapt"] = proposal
        self.world["adapts_used"] += 1
        self.world["last_line"] = DialogueLine(
            speaker="ZERO", intent="HINT", text=proposal.text, state="ADAPTING"
        )
        self.machine.go(SessionState.ACTIVE, reason="adapted")
        return self.snapshot()

    def complete(self, payload: dict[str, Any]) -> dict[str, Any]:
        if self.machine.state == SessionState.ADAPTING:
            self.machine.go(SessionState.ACTIVE, reason="complete")
        if self.machine.state != SessionState.ACTIVE:
            return self.snapshot({"error": "complete_not_allowed"})
        won = bool(payload.get("won"))
        failed = bool(payload.get("failed", not won))
        self.world["outcome"] = {"won": won, "failed": failed}
        self.machine.go(SessionState.COMPLETE, reason="engine")
        if won:
            line = self.invoke("npc_agent", {"persona": "zero", "intent": "SPEAK", "state": "COMPLETE", "plan": self.world["plan"]})
        else:
            line = self.invoke("npc_agent", {"persona": "iris", "intent": "SPEAK", "state": "FAIL", "plan": self.world["plan"]})
        self.world["last_line"] = line
        self.machine.go(SessionState.SAVING, reason="spoken")
        plan: MissionPlan | None = self.world["plan"]
        event = SessionEvent(
            kind="session_end",
            mission_id=plan.template_id if plan else None,
            duration_s=float(payload.get("duration_s", 0)),
            won=won,
            failed=failed,
            retries=int(payload.get("retries", 0)),
            hints=int(payload.get("hints", self.world["adapts_used"])),
            actions_per_min=float(payload.get("actions_per_min", 20)),
            favorite_surface=plan.primary_kind if plan else None,
            alarm_triggers=int(payload.get("alarm_triggers", 0)),
            template_id=plan.template_id if plan else None,
            difficulty=plan.difficulty if plan else None,
        )
        profile = self.invoke("player_model", {"profile": self.world["profile"], "event": event})
        if isinstance(profile, PlayerProfile):
            self.world["profile"] = profile
            self.store.save_player(profile)
        mission = {
            "plan": plan.model_dump() if plan else None,
            "puzzles": [p.model_dump() for p in self.world["puzzles"]],
            "outcome": self.world["outcome"],
        }
        self.store.save_last_mission(mission)
        flags = self.store.load_world()
        if plan:
            flags["last_primary_kind"] = plan.primary_kind
            flags["last_template_id"] = plan.template_id
            flags["met_iris"] = True
        self.store.save_world(flags)
        self.store.save_session({"id": uuid4().hex[:12], **event.model_dump()})
        self.machine.go(SessionState.READY_FOR_NEXT_SESSION, reason="saved")
        return self.snapshot()
