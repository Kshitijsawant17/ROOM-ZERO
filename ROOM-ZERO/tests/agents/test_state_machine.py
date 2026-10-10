from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "agents" / "zero"))

import pytest
from contracts import SessionState
from state_machine import IllegalTransition, StateMachine


def test_boot_to_room_ready():
    m = StateMachine()
    assert m.state == SessionState.BOOT
    m.go(SessionState.ROOM_READY, reason="boot")
    assert m.state == SessionState.ROOM_READY
    assert m.specialist() == "room_scout"


def test_illegal_skip_blocked():
    m = StateMachine()
    m.go(SessionState.ROOM_READY)
    with pytest.raises(IllegalTransition):
        m.go(SessionState.ACTIVE)


def test_full_legal_path():
    m = StateMachine()
    path = [
        SessionState.ROOM_READY,
        SessionState.MISSION_REQUESTED,
        SessionState.PLANNING,
        SessionState.VALIDATING,
        SessionState.BRIEFING,
        SessionState.ACTIVE,
        SessionState.ADAPTING,
        SessionState.ACTIVE,
        SessionState.COMPLETE,
        SessionState.SAVING,
        SessionState.READY_FOR_NEXT_SESSION,
    ]
    for nxt in path:
        m.go(nxt)
    assert m.state == SessionState.READY_FOR_NEXT_SESSION


def test_timeout_has_fallback():
    m = StateMachine()
    m.go(SessionState.ROOM_READY)
    m.entered_at -= 40
    assert m.timed_out()
    m.on_timeout()
    assert m.state == SessionState.ROOM_READY
