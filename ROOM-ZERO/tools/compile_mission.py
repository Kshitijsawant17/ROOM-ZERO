#!/usr/bin/env python3
from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "agents" / "zero"))

from session import ZeroSession  # noqa: E402


def main() -> None:
    room = json.loads((ROOT / "data" / "rooms" / "sample_bedroom.json").read_text())
    z = ZeroSession(ROOT / "data" / "memory")
    z.boot()
    z.handle("room_scan", {"room": room})
    snap = z.handle("give_mission", {})
    out = ROOT / "data" / "missions" / "generated_sample.json"
    out.write_text(json.dumps({"plan": snap["plan"], "puzzles": snap["puzzles"]}, indent=2))
    print(out)
    print(snap["machine"]["state"])


if __name__ == "__main__":
    main()
