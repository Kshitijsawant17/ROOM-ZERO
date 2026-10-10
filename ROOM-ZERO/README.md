# ROOM//ZERO

Your Room is Not a Level. Your Room is the Game Engine.

AI-generated heist games that run inside your real room. Built for the Meta VR Start Developer Competition 2026.

**Build mark: 50% complete.** Six brains are end-to-end: typed I/O, guardrails, one orchestrator, deterministic state machine. Device VR and filmed gameplay remain.

---

## Completed (50%)

### Concept, docs, competition pack
- Pitch, architecture, API, team, one-pager, credits
- 10-phase build plan with current mark
- Three-minute video shot list and voice script
- Submission checklist (unchecked items are remaining work)

### Six brain centers (end-to-end, no free-running loops)
See `docs/BRAINS.md`. One orchestrator. One specialist per state. Timeouts and fallbacks on every state.

BOOT -> ROOM_READY -> MISSION_REQUESTED -> PLANNING -> VALIDATING -> BRIEFING -> ACTIVE -> ADAPTING? -> COMPLETE -> SAVING -> READY_FOR_NEXT_SESSION

| Agent | Input | Output | Guardrail |
| --- | --- | --- | --- |
| Room Scout | scene, bounds, labels | RoomMap | no inferred hazards; unknown confidence; exclude doors/stairs/edges/blocked |
| Game Director | RoomMap, profile, catalog | MissionPlan | catalog only; clamp ranges; difficulty cooldown; never make safety harder |
| Puzzle Architect | RoomMap, catalog, difficulty | PuzzleSpec[] | anchor IDs only; no coordinates; max 3; clamped params |
| NPC Agent | state, persona, intent | DialogueLine + intent | enumerated intents; 25 words; fallback; no command execution |
| Player Model | session events | pace, retries, hints, prefs | telemetry only; strip personal data |
| Adaptation Agent | performance signals | one bounded proposal or null | cooldown; max 2; never make safety harder |

- HTTP: `/health` `/brains` `/state` `/boot` `/event` `/player` `/catalog`
- Catalogs in `config/catalogs/` and `config/guardrails.json`
- Validator rejects illegal plans before ACTIVE

### Deterministic game engine (Unity skeleton)
- Mission runner: timer, chained solves, win/lose, stuck telemetry
- Puzzles: code pad, wire, map align, keycard, circuit
- Alarm meter, hologram spawner, room scanner (editor fallback graph)
- Hands / passthrough / voice stubs, spatial voice, HUD, player memory on disk
- Agent client + ZERO bridge + live telemetry
- Bootstrap scene, Quest APK menu stub, package manifest

### Content
- Missions: desk vault, window ghost, shelf wire, table map
- Rooms: sample bedroom, sample office
- NPCs: ZERO, Iris, Voss
- Puzzle catalog including keycard

### Playable prototype (no Quest required)
- `prototype/index.html` — bedroom or office, click holograms, type 2046, three wires, map snap
- Adaptation after 25s stuck or Ask ZERO
- Player stats persist in the browser
- Tomorrow's mission avoids last vault kind

### Tools and tests
- `tools/simulate_room.py`, `tools/compile_mission.py`, `tools/pack_submit.sh`
- Agent tests: state machine, guardrails, session loop, pipeline, adaptation, player model

---

## Remaining (50%)

### Must have for the Quest demo
- Import Meta XR SDK (All-in-One) and OpenXR on Quest 3
- Real passthrough + scene understanding (walls, desks, floors) instead of sample JSON
- Spatial anchors so holograms stick to real furniture across sessions
- Hand tracking only: pinch, poke, grab. Controllers off in the submitted build
- On-device wake word "ZERO" plus speech-to-text for mission talk
- One complete live-room mission, start to extract, in a real apartment
- Spatial audio for ZERO / Iris / Voss attached to furniture
- Adaptation visible in headset (hint beam, timer ease) without a debug HUD

### Must have for Best Agentic AI
- Wire Unity AgentClient to a running orchestrator on device or companion PC
- LLM-authored mission text when `USER_LLM_API_KEY` is set (fallback already exists)
- NPC dialogue that uses the live room graph ("the left desk", "the window behind you")
- Prove agents veto illegal placements against the live mesh

### Must have for Best Reason to Return
- Player + world memory survive Quest app restart
- Second session in the same room generates a different layout and recalls one past choice
- Style / difficulty actually change after two recorded sessions on device

### Polish and submission
- Hologram materials, spawn animation, no jank
- Guardian / stairs / glass safety rejects
- Three-minute video of real headset gameplay (`video/SHOT_LIST.md`)
- Fill `docs/TEAM.md` names
- Android APK that installs on Quest 3
- Upload package per `submit/CHECKLIST.md`

### Out of scope until after demo
- Multiplayer
- Cloud memory
- Controllers
- Store page

---

## What this zip is

A working 50% software slice: agents generate missions from a room graph, memory persists, adaptation fires, and you can play the loop in the browser. It is not yet a Quest store build.

## Quick start — browser prototype

Open `prototype/index.html` in a browser.

1. Click Give me a mission
2. Click holograms in order (cyan = unlocked)
3. Code pad: type 2046
4. Wires: click the shelf three times
5. Map: click the table until north snaps
6. Wait 25s or click Ask ZERO to see adaptation
7. Refresh and run again: vault kind should change

## Quick start — agents

```bash
cd agents/zero
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cp ../../config/env.example .env
python orchestrator.py
```

```bash
python ../../tools/simulate_room.py
python -m pytest ../../tests/agents -q
```

Do not put platform LLM keys into this project. Use `USER_LLM_API_KEY` in your local `.env`.

## Repo layout

```
ROOM-ZERO/
  prototype/       Playable 50% heist loop in the browser
  agents/zero/     Six-brain AI + orchestrator
  unity/           Quest VR engine skeleton
  data/            Missions, puzzles, NPCs, rooms, memory
  config/          Agent, game, VR settings
  docs/            Pitch, architecture, build plan, status
  tools/           Room sim, mission compiler, zip packer
  video/           Shot list and script
  submit/          Competition checklist
  tests/           Agent tests
```

## Competition fit

| Award | Status at 50% |
| --- | --- |
| Best Agentic AI | Six agents + orchestrator exist and talk. Not yet driving a live Quest scene. |
| Boldest Concept | Design is spatial. Proof is the browser room graph + Unity stubs. Live passthrough remaining. |
| Best Reason to Return | Memory store + "avoid last vault" work in software. Not yet proven on-device overnight. |

## Requirements for the remaining 50%

- Unity 2022.3 LTS or Unity 6
- Meta XR SDK (All-in-One)
- Quest 3 / Quest 3S
- Python 3.11+ (already enough for agents)
- Player-provided OpenAI-compatible LLM key (optional)

## License

See `LICENSE`.
