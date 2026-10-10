from __future__ import annotations

import os
from pathlib import Path
from typing import Any

from dotenv import load_dotenv
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

from guardrails import ADAPT, MISSIONS, NPC, PUZZLES
from session import ZeroSession

load_dotenv()

ROOT = Path(__file__).resolve().parents[2]
MEMORY_DIR = Path(os.getenv("ZERO_MEMORY_DIR", ROOT / "data" / "memory"))
zero = ZeroSession(MEMORY_DIR)

app = FastAPI(title="ROOM//ZERO", version="0.6.0")
app.add_middleware(CORSMiddleware, allow_origins=["*"], allow_methods=["*"], allow_headers=["*"])


class EngineEvent(BaseModel):
    type: str
    payload: dict[str, Any] = Field(default_factory=dict)


@app.get("/health")
def health() -> dict[str, Any]:
    return {
        "status": "ok",
        "name": "ROOM//ZERO",
        "version": "0.6.0",
        "state": zero.machine.state.value,
        "brains": 6,
        "free_running": False,
    }


@app.get("/state")
def state() -> dict[str, Any]:
    return zero.snapshot()


@app.post("/boot")
def boot() -> dict[str, Any]:
    return zero.boot()


@app.post("/event")
def event(body: EngineEvent) -> dict[str, Any]:
    return zero.handle(body.type, body.payload)


@app.get("/player")
def player() -> dict[str, Any]:
    return zero.world["profile"].model_dump()


@app.get("/catalog")
def catalog() -> dict[str, Any]:
    return {"missions": MISSIONS, "puzzles": PUZZLES, "npc": NPC, "adaptation": ADAPT}


@app.get("/brains")
def brains() -> dict[str, Any]:
    return {
        "rule": "one orchestrator, one specialist per state, structured I/O, timeouts, fallbacks",
        "states": [
            "BOOT",
            "ROOM_READY",
            "MISSION_REQUESTED",
            "PLANNING",
            "VALIDATING",
            "BRIEFING",
            "ACTIVE",
            "ADAPTING",
            "COMPLETE",
            "SAVING",
            "READY_FOR_NEXT_SESSION",
        ],
        "agents": [
            {
                "id": "room_scout",
                "input": "RoomSceneInput",
                "output": "RoomMap",
                "state": "ROOM_READY",
                "guardrail": "never infer hidden hazards; mark unknown confidence; exclude doors, stairs, edges, blocked zones",
            },
            {
                "id": "player_model",
                "input": "SessionEvent | current profile",
                "output": "PlayerProfile",
                "state": ["MISSION_REQUESTED", "SAVING"],
                "guardrail": "game telemetry only; strip personal data",
            },
            {
                "id": "game_director",
                "input": "RoomMap + PlayerProfile",
                "output": "MissionPlan",
                "state": "PLANNING",
                "guardrail": "catalog templates only; clamp duration/difficulty; cooldown; never make safety harder",
            },
            {
                "id": "puzzle_architect",
                "input": "MissionPlan + RoomMap + puzzle catalog",
                "output": "PuzzleSpec[]",
                "state": "VALIDATING",
                "guardrail": "anchor IDs only; no arbitrary coordinates; bounded params; max 3 objects",
            },
            {
                "id": "npc_agent",
                "input": "persona + intent + state + MissionPlan",
                "output": "DialogueLine",
                "state": ["BRIEFING", "COMPLETE"],
                "guardrail": "enumerated intents only; 25 words; fallback line; no command execution",
            },
            {
                "id": "adaptation",
                "input": "PerformanceSignals",
                "output": "AdaptationProposal | null",
                "state": "ADAPTING",
                "guardrail": "one bounded proposal; cooldown; never make safety harder",
            },
        ],
    }


if __name__ == "__main__":
    import uvicorn

    host = os.getenv("ZERO_HOST", "127.0.0.1")
    port = int(os.getenv("ZERO_PORT", "8765"))
    uvicorn.run("orchestrator:app", host=host, port=port, reload=False)
