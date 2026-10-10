from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "agents" / "zero"))

from agents.adaptation import AdaptationAgent
from contracts import PerformanceSignals


def test_stuck_emits_hint():
    out = AdaptationAgent().run(
        PerformanceSignals(stuck_s=30, idle_s=0, puzzles_left=1, last_adapt_s=40, adapts_used=0),
        {},
    )
    assert out is not None
    assert out.kind in {"HINT_BEAM", "EASE_TIMER", "SKIP_OPTIONAL", "LOWER_DIFFICULTY"}
    assert out.safety_ok


def test_cooldown_blocks():
    out = AdaptationAgent().run(
        PerformanceSignals(stuck_s=30, last_adapt_s=5, adapts_used=0),
        {},
    )
    assert out is None
