# Build Plan — 10 Phases

Target: Meta VR Start Developer Competition 2026.

Current mark: Phase 5 complete in software. Phase 6 starts on device.

## Week 1 — Phase 1-2 — DONE (software)

VR foundation. Scene understanding. Hand tracking working.

- Unity project skeleton, Android / Quest player settings
- Passthrough rig, hand controller, voice wake stub
- Room graph schema + sample bedroom / office scans
- Empty scene: "ZERO is listening"
- Remaining on device: Meta XR SDK import, real scene mesh

## Week 1-2 — Phase 3-4 — DONE (software)

Basic spatial objects. Puzzles. Deterministic game engine.

- Hologram spawner + mission runner with timer, chain solves, win/lose
- Puzzle primitives: code pad, wire, map align, keycard, circuit
- Alarm meter
- Voice command stub: "give me a mission"
- Playable browser prototype of the same loop
- Remaining on device: holograms locked to live spatial anchors

## Week 2 — Phase 5-6 — 5 DONE / 6 IN PROGRESS

AI agents built. ZERO orchestrator working. One mission playable end-to-end.

- Six agents with JSON schemas and a live bus
- Orchestrator HTTP API (health, event, player, world, catalog)
- Four authored missions plus generated missions from room graphs
- NPC lines for ZERO, Iris, Voss
- Remaining: NPC voice over spatial audio on Quest, one live-room mission

## Week 2-3 — Phase 7-8 — PARTIAL

AI generates missions. Memory system persists player data.

- Game Director samples objectives from room graph and avoids last surface
- Puzzle Architect places chained puzzles only on safe surfaces
- Player Model writes session stats, style, difficulty
- Memory store for player, world flags, last mission, sessions
- Remaining: on-headset persistence + opt-in cloud is out of scope

## Week 3 — Phase 9-10 — NOT STARTED ON DEVICE

Adaptive AI. Polish. Demo-ready.

- Adaptation Agent exists (hint, ease timer, skip optional, raise alert)
- Remaining: Quest polish, spatial audio mix, three-minute capture, submission upload

## Roles

| Role | Owns |
| --- | --- |
| Lead Developer | Unity engine, VR foundation, hands, passthrough, anchors |
| AI/Agent Engineer | Six agents, bus, orchestration |
| Game Designer | Mission types, puzzle logic, NPC behavior, difficulty |
| VR/UX Designer | Hand UX, voice UX, spatial audio |
| Integration Lead | Engine <-> agents, APIs, build |

## Exit Criteria for Demo

- Quest app scans one room and completes one mission
- Hands only
- ZERO adapts once when the player is stuck
- Memory survives app restart
- Video is real gameplay, not a slideshow
