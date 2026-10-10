from __future__ import annotations

import json
import time
from pathlib import Path
from typing import Any, Callable

from contracts import SessionState

ROOT = Path(__file__).resolve().parents[2]
SPEC = json.loads((ROOT / "config" / "state_machine.json").read_text())


class IllegalTransition(RuntimeError):
    pass


class StateMachine:
    def __init__(self) -> None:
        self.state = SessionState.BOOT
        self.entered_at = time.monotonic()
        self.retries = 0
        self.history: list[dict[str, Any]] = []
        self.last_error: str | None = None

    def elapsed_ms(self) -> float:
        return (time.monotonic() - self.entered_at) * 1000

    def spec(self, state: SessionState | None = None) -> dict[str, Any]:
        return SPEC["states"][state.value if state else self.state.value]

    def timeout_ms(self) -> int:
        return int(self.spec()["timeout_ms"])

    def specialist(self) -> str | None:
        return self.spec().get("specialist")

    def timed_out(self) -> bool:
        limit = self.timeout_ms()
        return limit > 0 and self.elapsed_ms() > limit

    def can_go(self, nxt: SessionState) -> bool:
        allowed = SPEC["transitions"].get(self.state.value, [])
        return nxt.value in allowed

    def go(self, nxt: SessionState, reason: str = "") -> SessionState:
        if nxt == self.state:
            self.entered_at = time.monotonic()
            return self.state
        if not self.can_go(nxt):
            raise IllegalTransition(f"{self.state.value} -> {nxt.value}")
        self.history.append(
            {
                "from": self.state.value,
                "to": nxt.value,
                "reason": reason,
                "ms": round(self.elapsed_ms(), 1),
            }
        )
        self.state = nxt
        self.entered_at = time.monotonic()
        self.retries = 0
        self.last_error = None
        return self.state

    def on_error(self, message: str) -> SessionState:
        self.last_error = message
        self.retries += 1
        spec = self.spec()
        fallback = SessionState(spec["on_error"])
        if self.retries > int(spec.get("max_retries", 0)) and fallback != self.state:
            return self.go(fallback, reason=f"error:{message}")
        if fallback != self.state:
            return self.go(fallback, reason=f"error:{message}")
        self.entered_at = time.monotonic()
        return self.state

    def on_timeout(self) -> SessionState:
        nxt = SessionState(self.spec()["on_timeout"])
        if nxt == self.state:
            self.entered_at = time.monotonic()
            return self.state
        if self.can_go(nxt):
            return self.go(nxt, reason="timeout")
        return self.on_error("timeout-illegal")

    def snapshot(self) -> dict[str, Any]:
        return {
            "state": self.state.value,
            "elapsed_ms": round(self.elapsed_ms(), 1),
            "timeout_ms": self.timeout_ms(),
            "specialist": self.specialist(),
            "retries": self.retries,
            "last_error": self.last_error,
            "history": self.history[-16:],
        }


def with_timeout(fn: Callable[[], Any], timeout_s: float, fallback: Any) -> Any:
    start = time.monotonic()
    try:
        result = fn()
    except Exception:
        return fallback
    if time.monotonic() - start > timeout_s:
        return fallback
    return result
