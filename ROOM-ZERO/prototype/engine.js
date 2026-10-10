(function () {
  const canvas = document.getElementById("room");
  const ctx = canvas.getContext("2d");
  const hudTitle = document.getElementById("title");
  const hudObj = document.getElementById("objective");
  const hudTimer = document.getElementById("timer");
  const alarmEl = document.getElementById("alarm");
  const logEl = document.getElementById("log");
  const stage = document.getElementById("stage");

  const PUZZLES = {
    desk: ["code_pad", "HoloCodePad"],
    table: ["map_align", "HoloMap"],
    shelf: ["wire", "HoloWire"],
    wall: ["circuit", "HoloCircuit"],
    window: ["token", "HoloToken"],
    door: ["keycard", "HoloKeycard"]
  };

  const player = {
    id: "local",
    sessions: Number(localStorage.getItem("rz_sessions") || 0),
    wins: Number(localStorage.getItem("rz_wins") || 0),
    fails: Number(localStorage.getItem("rz_fails") || 0),
    style: localStorage.getItem("rz_style") || "stealth",
    lastKind: localStorage.getItem("rz_last_kind") || ""
  };

  let roomKey = "bedroom";
  let room = window.ROOMZERO_ROOMS[roomKey];
  let mission = null;
  let placements = [];
  let solved = new Set();
  let timer = 0;
  let running = false;
  let alarm = 0;
  let stuck = 0;
  let adapted = false;
  let padBuffer = "";
  let wires = 0;
  let mapYaw = 40;
  let lastTick = performance.now();

  function log(line) {
    const p = document.createElement("div");
    p.textContent = line;
    logEl.prepend(p);
  }

  function pickPrimary(surfaces) {
    const ranked = surfaces.slice().sort((a, b) => b.area_m2 - a.area_m2);
    return ranked.find((s) => s.kind !== player.lastKind) || ranked[0];
  }

  function buildMission() {
    const surfaces = room.surfaces.filter((s) => s.safe);
    const primary = pickPrimary(surfaces);
    const difficulty = player.fails >= 2 ? "easy" : player.wins >= 1 && player.fails === 0 ? "hard" : "normal";
    const duration = { easy: 210, normal: 180, hard: 140 }[difficulty];
    const limit = { easy: 2, normal: 3, hard: 3 }[difficulty];
    const used = [];
    const place = [];
    surfaces.slice(0, limit).forEach((s, i) => {
      const pair = PUZZLES[s.kind] || ["token", "HoloToken"];
      const id = pair[0] + "_" + s.id;
      place.push({
        puzzle_id: id,
        puzzle_type: pair[0],
        prefab: pair[1],
        surface_id: s.id,
        surface: s,
        requires: used[i - 1] || null,
        solved: false
      });
      used.push(id);
    });
    const callback = player.lastKind
      ? "ZERO: Last time this " + player.lastKind + " was the vault. Not tonight."
      : "ZERO: Room is live. Stay quiet.";
    return {
      id: "web_" + Date.now().toString(36),
      title: player.style[0].toUpperCase() + player.style.slice(1) + " on the " + primary.kind,
      duration_s: duration,
      difficulty,
      objective: "Crack the " + primary.kind + " and extract the drive.",
      primary_kind: primary.kind,
      intro: callback,
      placements: place
    };
  }

  function startMission() {
    room = window.ROOMZERO_ROOMS[roomKey];
    mission = buildMission();
    placements = mission.placements;
    solved = new Set();
    timer = mission.duration_s;
    running = true;
    alarm = 0;
    stuck = 0;
    adapted = false;
    padBuffer = "";
    wires = 0;
    mapYaw = 40;
    stage.classList.remove("win", "fail");
    hudTitle.textContent = mission.title;
    hudObj.textContent = mission.objective;
    log(mission.intro);
    log("Iris: Largest safe surface. That is the vault.");
    persistPlayer();
  }

  function persistPlayer() {
    localStorage.setItem("rz_sessions", String(player.sessions));
    localStorage.setItem("rz_wins", String(player.wins));
    localStorage.setItem("rz_fails", String(player.fails));
    localStorage.setItem("rz_style", player.style);
    localStorage.setItem("rz_last_kind", player.lastKind);
  }

  function end(win) {
    running = false;
    player.sessions += 1;
    if (win) player.wins += 1;
    else player.fails += 1;
    player.lastKind = mission.primary_kind;
    persistPlayer();
    stage.classList.add(win ? "win" : "fail");
    hudTitle.textContent = win ? "EXTRACTED" : "COMPROMISED";
    log(win ? "ZERO: Tomorrow this room will not be the same." : "Iris: ZERO will remember that. I already did.");
  }

  function canSolve(p) {
    if (!p.requires) return true;
    return solved.has(p.requires);
  }

  function complete(p) {
    if (!running || p.solved) return;
    if (!canSolve(p)) {
      alarm = Math.min(1, alarm + 0.18);
      log("Iris: Wrong order. The " + p.surface.kind + " is locked.");
      return;
    }
    p.solved = true;
    solved.add(p.puzzle_id);
    stuck = 0;
    log("ZERO: " + p.puzzle_type + " clear.");
    if (solved.size >= placements.length) end(true);
  }

  function adapt() {
    if (adapted) return;
    adapted = true;
    timer += 20;
    log("ZERO: I moved the vent. Go.");
    const optional = placements.find((p) => !p.solved && p.requires);
    if (optional) {
      optional.solved = true;
      solved.add(optional.puzzle_id);
      log("Iris: Skip the extra node. Vault still counts.");
    }
  }

  function hitTest(mx, my) {
    const w = canvas.width;
    const h = canvas.height;
    for (let i = placements.length - 1; i >= 0; i--) {
      const p = placements[i];
      const s = p.surface;
      const x = s.x * w;
      const y = s.y * h;
      const bw = s.w * w;
      const bh = s.h * h;
      if (mx >= x && mx <= x + bw && my >= y && my <= y + bh) return p;
    }
    return null;
  }

  canvas.addEventListener("click", (ev) => {
    if (!running) return;
    const rect = canvas.getBoundingClientRect();
    const mx = ((ev.clientX - rect.left) / rect.width) * canvas.width;
    const my = ((ev.clientY - rect.top) / rect.height) * canvas.height;
    const p = hitTest(mx, my);
    if (!p) return;
    if (p.puzzle_type === "code_pad") {
      log("Pad waiting. Type 2046.");
      return;
    }
    if (p.puzzle_type === "wire") {
      wires += 1;
      log("Wire " + wires + "/3");
      if (wires >= 3) complete(p);
      return;
    }
    if (p.puzzle_type === "map_align") {
      mapYaw = (mapYaw + 25) % 360;
      log("Map yaw " + Math.round(mapYaw));
      if (mapYaw < 12 || mapYaw > 348) complete(p);
      return;
    }
    complete(p);
  });

  window.addEventListener("keydown", (ev) => {
    if (!running) return;
    if ("2046".includes(ev.key)) {
      padBuffer += ev.key;
      log("Pad " + padBuffer);
      if (padBuffer.length >= 4) {
        const pad = placements.find((p) => p.puzzle_type === "code_pad" && !p.solved);
        if (padBuffer === "2046" && pad) complete(pad);
        else {
          alarm = Math.min(1, alarm + 0.25);
          log("Voss: I heard that.");
        }
        padBuffer = "";
      }
    }
  });

  document.getElementById("btn-mission").onclick = startMission;
  document.getElementById("btn-hint").onclick = function () {
    if (!running) {
      log("ZERO: Say give me a mission.");
      return;
    }
    adapt();
  };
  document.getElementById("btn-scan").onclick = function () {
    log("ROOM SCOUT: " + room.surfaces.length + " surfaces. Open volume mapped.");
  };
  document.getElementById("room-select").onchange = function (e) {
    roomKey = e.target.value;
    room = window.ROOMZERO_ROOMS[roomKey];
    log("ROOM SCOUT: switched to " + room.room_id);
  };

  function draw() {
    const w = canvas.width;
    const h = canvas.height;
    ctx.fillStyle = "#0b1118";
    ctx.fillRect(0, 0, w, h);
    ctx.strokeStyle = "#1d3a4d";
    ctx.strokeRect(20, 20, w - 40, h - 40);
    room.surfaces.forEach((s) => {
      ctx.fillStyle = "rgba(30, 70, 90, 0.35)";
      ctx.fillRect(s.x * w, s.y * h, s.w * w, s.h * h);
      ctx.strokeStyle = "#3a6a82";
      ctx.strokeRect(s.x * w, s.y * h, s.w * w, s.h * h);
      ctx.fillStyle = "#7ea0b8";
      ctx.font = "12px sans-serif";
      ctx.fillText(s.kind.toUpperCase(), s.x * w + 6, s.y * h + 16);
    });
    placements.forEach((p) => {
      const s = p.surface;
      ctx.strokeStyle = p.solved ? "#7dffb3" : canSolve(p) ? "#3cf0ff" : "#445";
      ctx.lineWidth = 2;
      ctx.strokeRect(s.x * w + 6, s.y * h + 6, s.w * w - 12, s.h * h - 12);
      ctx.fillStyle = p.solved ? "#7dffb3" : "#3cf0ff";
      ctx.font = "11px sans-serif";
      ctx.fillText(p.puzzle_type, s.x * w + 10, s.y * h + s.h * h - 10);
    });
  }

  function tick(now) {
    const dt = (now - lastTick) / 1000;
    lastTick = now;
    if (running) {
      timer -= dt;
      stuck += dt;
      alarm = Math.max(0, alarm - dt * 0.02);
      if (stuck >= 25) adapt();
      if (timer <= 0 || alarm >= 1) end(false);
      hudTimer.textContent = Math.max(0, Math.ceil(timer)) + "s  |  " + mission.difficulty + "  |  " + player.style;
      alarmEl.style.setProperty("--w", Math.round(alarm * 100) + "%");
    }
    draw();
    requestAnimationFrame(tick);
  }

  log("ZERO is listening. Hands and keys only.");
  requestAnimationFrame(tick);
})();
