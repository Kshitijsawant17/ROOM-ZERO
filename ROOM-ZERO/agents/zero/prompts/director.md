# Game Director Prompt

Given a RoomGraph and a PlayerModel, output one Mission JSON.

Constraints:
- duration_s between 120 and 240
- placements only on safe surfaces
- one primary objective
- difficulty from player.suggested_difficulty unless Adaptation vetoes
- seed derived from trace_id
- no objects on hazards

Return JSON only.
