# Agent API

Local only. Default `http://127.0.0.1:8765`.

One orchestrator. One specialist per state. No free-running chat loops.

## GET /health

```json
{ "status": "ok", "name": "ROOM//ZERO", "version": "0.6.0", "state": "ROOM_READY", "free_running": false }
```

## GET /brains

Documents the six interfaces, states, and guardrails.

## GET /state

Current machine snapshot: state, plan, puzzles, profile, adapt, invoke log.

## POST /boot

BOOT -> ROOM_READY.

## POST /event

```json
{ "type": "room_scan | give_mission | live | complete | timeout", "payload": {} }
```

| type | legal states | specialist |
| --- | --- | --- |
| room_scan | ROOM_READY | room_scout |
| give_mission | ROOM_READY, READY_FOR_NEXT_SESSION | player_model then director then architect then npc |
| live | ACTIVE | adaptation only if stuck/idle/retries trip ADAPTING |
| complete | ACTIVE | npc then player_model |
| timeout | any | uses that state's on_timeout |

Engine validates every PuzzleSpec against the live RoomMap before spawn.

## GET /player

Telemetry-only PlayerProfile.

## GET /catalog

Mission templates, puzzles, NPC intents, adaptation kinds.
