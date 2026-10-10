# Six Brain Centers — Scope and Safe Interfaces

Agents are not six independent free-running chat loops.

One orchestrator owns explicit states, timeouts, structured inputs/outputs, and a deterministic mission state machine. Only the specialist for the current step is invoked.

## State machine

BOOT -> ROOM_READY -> MISSION_REQUESTED -> PLANNING -> VALIDATING -> BRIEFING -> ACTIVE -> ADAPTING (optional) -> COMPLETE -> SAVING -> READY_FOR_NEXT_SESSION

Every state has `timeout_ms`, `on_timeout`, `on_error`, and a single `specialist` (or none). Illegal transitions raise. See `config/state_machine.json`.

| State | Specialist | Timeout | Timeout / error fallback |
| --- | --- | --- | --- |
| BOOT | none | 3s | BOOT |
| ROOM_READY | room_scout | 30s | ROOM_READY |
| MISSION_REQUESTED | player_model | 2s | PLANNING / ROOM_READY |
| PLANNING | game_director | 1.5s | ROOM_READY |
| VALIDATING | puzzle_architect | 0.8s | PLANNING |
| BRIEFING | npc_agent | 8s | ACTIVE |
| ACTIVE | none (engine) | 240s | COMPLETE |
| ADAPTING | adaptation | 0.4s | ACTIVE |
| COMPLETE | npc_agent | 2s | SAVING |
| SAVING | player_model | 1s | READY_FOR_NEXT_SESSION |
| READY_FOR_NEXT_SESSION | none | 0 | ROOM_READY |

## Interfaces

| Agent | Input | Output | Guardrail |
| --- | --- | --- | --- |
| Room Scout | Room/scene data, bounds, surface labels | Normalized RoomMap | Never infer hidden hazards. Mark unknown confidence. Exclude doors, stairs, edges, blocked zones. |
| Game Director | RoomMap, PlayerProfile, mission catalog | MissionPlan: theme, objective IDs, difficulty, duration | Catalog templates only. Clamp ranges. Cool-down between difficulty jumps. Never make safety harder. |
| Puzzle Architect | Valid surfaces/zones, puzzle catalog, difficulty | PuzzleSpec list with target anchor IDs and clamped params | No arbitrary coordinates. No unbounded spawning. Max 3 objects. Catalog params only. |
| NPC Agent | Mission state, persona, enumerated intent | Dialogue line + enumerated behavior intent | No free-form command execution. Max 25 words. Fallback dialogue. Intents: SPEAK HINT WAIT ACKNOWLEDGE URGE FALLBACK. |
| Player Model | Session events and past summaries | Profile: pace, retries, hints, preferences | Game telemetry only. Strip name, email, voice, photo, mesh, location, device_id. |
| Adaptation Agent | Performance signals, current difficulty | One bounded adjustment proposal or null | Cooldown 25s. Max 2 per mission. Never make safety harder. Forbidden: disable guardian, raise hazard, unbounded spawn, shorten safety margin. |

## What the engine does

The Unity / prototype engine is the source of truth in ACTIVE. Agents never move objects. The validator rejects any PuzzleSpec whose anchor is not playable, whose prefab is not in catalog, or whose params include coordinates.

## Catalogs

- `config/catalogs/missions.json` — allowed templates and objective types
- `config/catalogs/puzzles.json` — allowed puzzles, surfaces, param bounds
- `config/catalogs/npc.json` — intents, personas, fallback lines
- `config/catalogs/adaptation.json` — allowed / forbidden kinds and magnitudes
- `config/guardrails.json` — numeric clamps per agent
