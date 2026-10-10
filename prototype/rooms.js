window.ROOMZERO_ROOMS = {
  bedroom: {
    room_id: "sample_bedroom",
    bounds: { width_m: 4.2, depth_m: 3.6, height_m: 2.5 },
    surfaces: [
      { id: "desk_a", kind: "desk", area_m2: 1.2, x: 0.18, y: 0.55, w: 0.28, h: 0.18, safe: true },
      { id: "table_a", kind: "table", area_m2: 1.8, x: 0.52, y: 0.58, w: 0.32, h: 0.2, safe: true },
      { id: "shelf_a", kind: "shelf", area_m2: 0.4, x: 0.08, y: 0.18, w: 0.18, h: 0.28, safe: true },
      { id: "wall_n", kind: "wall", area_m2: 8.0, x: 0.0, y: 0.0, w: 1, h: 0.12, safe: true },
      { id: "window_a", kind: "window", area_m2: 1.1, x: 0.7, y: 0.12, w: 0.18, h: 0.16, safe: true }
    ],
    volumes: [{ id: "open_center", kind: "open", volume_m3: 12 }]
  },
  office: {
    room_id: "sample_office",
    bounds: { width_m: 5.1, depth_m: 4.0, height_m: 2.7 },
    surfaces: [
      { id: "desk_l", kind: "desk", area_m2: 1.6, x: 0.08, y: 0.5, w: 0.3, h: 0.18, safe: true },
      { id: "table_meet", kind: "table", area_m2: 2.4, x: 0.42, y: 0.48, w: 0.36, h: 0.22, safe: true },
      { id: "shelf_b", kind: "shelf", area_m2: 0.6, x: 0.82, y: 0.2, w: 0.12, h: 0.4, safe: true },
      { id: "wall_e", kind: "wall", area_m2: 10.2, x: 0.92, y: 0.0, w: 0.08, h: 1, safe: true },
      { id: "door_a", kind: "door", area_m2: 1.8, x: 0.02, y: 0.2, w: 0.08, h: 0.36, safe: true }
    ],
    volumes: [{ id: "open_center", kind: "open", volume_m3: 22 }]
  }
};
