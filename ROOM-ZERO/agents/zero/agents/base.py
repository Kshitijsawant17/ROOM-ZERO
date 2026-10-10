from __future__ import annotations

from abc import ABC, abstractmethod
from typing import Any, Generic, TypeVar

InT = TypeVar("InT")
OutT = TypeVar("OutT")


class Specialist(ABC, Generic[InT, OutT]):
    id: str
    timeout_ms: int

    def __init__(self, agent_id: str, timeout_ms: int) -> None:
        self.id = agent_id
        self.timeout_ms = timeout_ms

    @abstractmethod
    def run(self, payload: InT, ctx: dict[str, Any]) -> OutT:
        raise NotImplementedError
