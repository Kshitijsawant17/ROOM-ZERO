#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[1]


def post(url: str, payload: dict) -> dict:
    req = Request(
        url,
        data=json.dumps(payload).encode(),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urlopen(req, timeout=5) as resp:
        return json.loads(resp.read().decode())


def main() -> None:
    parser = argparse.ArgumentParser(description="Drive ZERO through one mission.")
    parser.add_argument("--room", default=str(ROOT / "data" / "rooms" / "sample_bedroom.json"))
    parser.add_argument("--url", default="http://127.0.0.1:8765")
    args = parser.parse_args()
    room = json.loads(Path(args.room).read_text())
    try:
        post(args.url + "/boot", {})
        scan = post(args.url + "/event", {"type": "room_scan", "payload": {"room": room}})
        print("state", scan["machine"]["state"], "playable", len(scan["room"]["playable"]) if scan.get("room") else 0)
        mission = post(args.url + "/event", {"type": "give_mission", "payload": {}})
        print("state", mission["machine"]["state"], "plan", mission.get("plan"))
    except Exception as exc:
        print("agent offline:", exc)
        print(json.dumps({"type": "room_scan", "payload": {"room": room}}, indent=2))


if __name__ == "__main__":
    main()
