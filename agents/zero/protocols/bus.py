from __future__ import annotations

from collections import defaultdict, deque
from typing import Callable, Deque, Dict, List

from .message import AgentMessage


class AgentBus:
    def __init__(self) -> None:
        self._queues: Dict[str, Deque[AgentMessage]] = defaultdict(deque)
        self._handlers: Dict[str, List[Callable[[AgentMessage], None]]] = defaultdict(list)
        self.history: List[AgentMessage] = []

    def subscribe(self, agent_id: str, handler: Callable[[AgentMessage], None]) -> None:
        self._handlers[agent_id].append(handler)

    def publish(self, message: AgentMessage) -> None:
        self.history.append(message)
        targets = [message.to] if message.to != "*" else list(self._handlers.keys())
        for target in targets:
            self._queues[target].append(message)
            for handler in self._handlers.get(target, []):
                handler(message)

    def drain(self, agent_id: str) -> List[AgentMessage]:
        q = self._queues[agent_id]
        items = list(q)
        q.clear()
        return items
