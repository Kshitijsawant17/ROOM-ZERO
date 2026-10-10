# Architecture

ROOM//ZERO splits into a deterministic engine on device and one orchestrator that calls six specialist brains. Agents never run as chat loops. The engine is the source of truth for physics, collision, and win/lose.

## High Level

```
Player (Quest 3, passthrough)
        |
        v
Unity Engine  --spatial anchors, hands, voice--  Room
        |
        |  JSON over local HTTP
        v
ZERO Orchestrator  (state machine + timeouts)
        |
        |  one specialist per state
        +-- Room Scout        ROOM_READY
        +-- Player Model      MISSION_REQUESTED, SAVING
        +-- Game Director     PLANNING
        +-- Puzzle Architect  VALIDATING
        +-- NPC Agent         BRIEFING, COMPLETE
        +-- Adaptation Agent  ADAPTING
        |
        v
Persistent Memory Store (local JSON, telemetry only)
```

## Determinism Rule

Specialists emit typed objects (`RoomMap`, `MissionPlan`, `PuzzleSpec`, `DialogueLine`, `PlayerProfile`, `AdaptationProposal`). The validator and the engine apply only legal ones. If an architect targets an excluded door, VALIDATING fails and the machine falls back to PLANNING.

## State Machine

BOOT -> ROOM_READY -> MISSION_REQUESTED -> PLANNING -> VALIDATING -> BRIEFING -> ACTIVE -> ADAPTING? -> COMPLETE -> SAVING -> READY_FOR_NEXT_SESSION

Illegal transitions raise `IllegalTransition`. Every state has a timeout and an error fallback. Invoking the wrong specialist in a state raises `specialist_not_allowed`.

Details: `docs/BRAINS.md` and `config/state_machine.json`.

## RoomMap

Produced by Room Scout. Playable anchors only.

```
RoomMap
  bounds
  playable[]     desk, table, shelf, floor, wall, window, chair + anchor_id + confidence
  excluded[]     doors, stairs, edges, blocked, low-confidence, unconfirmed hazards
  unknown_count
  ready
```

Hazards without `source=sensor` are excluded as unconfirmed. They are never inferred.

## MissionPlan

Catalog template only: theme, objective_ids, difficulty, duration_s, primary_anchor_id.

## PuzzleSpec

`target_anchor_id` plus clamped catalog params. No x/y/z. Max three objects. Chain via `requires`.

## Memory

Three layers, telemetry only:

1. Session — in RAM
2. Player — pace, retries, hints, style, difficulty, history
3. World — last template, last vault kind, met_iris

Forbidden in storage: name, email, voice, photo, mesh, location, device_id.

## Voice and Hands

- Voice wake "ZERO" requests a mission (MISSION_REQUESTED)
- Hands: pinch, poke, grab
- No controllers in the submitted demo
- NPC never executes commands from speech

## Networking

Demo runs local. Agent service binds `127.0.0.1`. No free-running LLM loops; optional LLM is a future fill-in behind the same typed outputs.

## Security

- No secrets in repo
- Player LLM key via `USER_LLM_API_KEY` only if enabled later
- Room scans stay on device
- Agent prompts never include raw camera frames in logs
