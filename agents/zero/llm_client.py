from __future__ import annotations

import os
from typing import Any

import httpx


class LlmClient:
    def __init__(self) -> None:
        self.api_key = os.getenv("USER_LLM_API_KEY", "")
        self.base_url = os.getenv("USER_LLM_BASE_URL", "").rstrip("/")
        self.model = os.getenv("USER_LLM_MODEL", "your-model-name")

    @property
    def enabled(self) -> bool:
        return bool(self.api_key and self.base_url)

    def complete(self, system: str, user: str) -> str | None:
        if not self.enabled:
            return None
        url = f"{self.base_url}/chat/completions"
        headers = {"Authorization": f"Bearer {self.api_key}", "Content-Type": "application/json"}
        body: dict[str, Any] = {
            "model": self.model,
            "temperature": 0.7,
            "max_tokens": 400,
            "messages": [
                {"role": "system", "content": system},
                {"role": "user", "content": user},
            ],
        }
        try:
            with httpx.Client(timeout=12.0) as client:
                resp = client.post(url, headers=headers, json=body)
                resp.raise_for_status()
                data = resp.json()
            return data["choices"][0]["message"]["content"]
        except Exception:
            return None
