from __future__ import annotations

from typing import Any, Literal
from uuid import uuid4

from pydantic import BaseModel, Field

MessageType = Literal["observe", "propose", "reject", "commit", "adapt", "remember"]


class AgentMessage(BaseModel):
    sender: str = Field(alias="from")
    to: str
    type: MessageType
    trace_id: str = Field(default_factory=lambda: uuid4().hex)
    priority: int = 50
    payload: dict[str, Any] = Field(default_factory=dict)

    model_config = {"populate_by_name": True}

    def reply(self, sender: str, type: MessageType, payload: dict[str, Any], priority: int = 50) -> "AgentMessage":
        return AgentMessage(
            sender=sender,
            to=self.sender,
            type=type,
            trace_id=self.trace_id,
            priority=priority,
            payload=payload,
        )
