using System;
using System.Collections.Generic;
using UnityEngine;
using RoomZero.Zero;

namespace RoomZero.Spatial
{
    // Builds the RoomGraph the agents plan against. On Quest: MRUK scene model.
    // In the editor or without a scene: a fixed sample room.
    public sealed class RoomScanner : MonoBehaviour
    {
        [SerializeField] AgentClient client;
        [SerializeField] float minSurfaceArea = 0.15f;
        [Tooltip("Editor stand-in furniture. Each transform's name is its anchor id (a1, a2, ...).")]
        [SerializeField] Transform[] editorAnchors;
        public RoomGraph LastGraph { get; private set; }
        public event Action<RoomGraph> OnScanned;

        readonly Dictionary<string, Transform> _anchors = new Dictionary<string, Transform>();

        public bool TryGetAnchor(string anchorId, out Transform anchor)
        {
            return _anchors.TryGetValue(anchorId ?? "", out anchor) && anchor != null;
        }

        public void Scan()
        {
#if META_MRUK
            var mruk = Meta.XR.MRUtilityKit.MRUK.Instance;
            if (mruk != null && !Application.isEditor)
            {
                if (mruk.GetCurrentRoom() != null)
                    Publish(BuildFromScene(mruk.GetCurrentRoom()));
                else
                    mruk.RegisterSceneLoadedCallback(() => Publish(BuildFromScene(mruk.GetCurrentRoom())));
                return;
            }
#endif
            Publish(BuildEditorFallback());
        }

        void Publish(RoomGraph graph)
        {
            LastGraph = graph;
            if (client != null)
                client.SendRoomScan(LastGraph);
            OnScanned?.Invoke(LastGraph);
        }

#if META_MRUK
        RoomGraph BuildFromScene(Meta.XR.MRUtilityKit.MRUKRoom room)
        {
            _anchors.Clear();
            var graph = new RoomGraph
            {
                room_id = room != null ? room.name : "quest_room",
                scanned_at = DateTime.UtcNow.ToString("o"),
                bounds = new RoomBounds()
            };
            if (room == null)
                return graph;

            float floorY = room.FloorAnchors.Count > 0 ? room.FloorAnchors[0].transform.position.y : 0f;
            var roomBounds = room.GetRoomBounds();
            graph.bounds.width_m = roomBounds.size.x;
            graph.bounds.depth_m = roomBounds.size.z;
            graph.bounds.height_m = roomBounds.size.y;

            int n = 0;
            foreach (var a in room.Anchors)
            {
                string label = a.Label.ToString().ToUpperInvariant();
                string kind = MapKind(label);
                if (kind == null)
                    continue;
                string id = kind + "_" + (n++);
                string anchorId = "a" + n;
                _anchors[anchorId] = a.transform;

                if (kind == "door" || kind == "window")
                {
                    graph.volumes.Add(new RoomVolume { id = id, kind = "blocked", volume_m3 = 0f });
                    graph.surfaces.Add(new RoomSurface { id = id, kind = kind, anchor_id = anchorId, safe = false,
                        height_m = a.transform.position.y - floorY });
                    continue;
                }

                float area = 0f;
                if (a.PlaneRect.HasValue) area = a.PlaneRect.Value.width * a.PlaneRect.Value.height;
                else if (a.VolumeBounds.HasValue) area = a.VolumeBounds.Value.size.x * a.VolumeBounds.Value.size.y;
                float top = a.VolumeBounds.HasValue
                    ? a.transform.TransformPoint(a.VolumeBounds.Value.center + Vector3.forward * a.VolumeBounds.Value.extents.z).y
                    : a.transform.position.y;

                graph.surfaces.Add(new RoomSurface
                {
                    id = id,
                    kind = kind,
                    anchor_id = anchorId,
                    area_m2 = area,
                    height_m = top - floorY,
                    safe = area >= minSurfaceArea
                });
            }
            float open = graph.bounds.width_m * graph.bounds.depth_m * 0.4f * graph.bounds.height_m;
            graph.volumes.Add(new RoomVolume { id = "open_center", kind = "open", volume_m3 = open });
            return graph;
        }

        static string MapKind(string label)
        {
            if (label.Contains("TABLE")) return "desk";
            if (label.Contains("STORAGE")) return "shelf";
            if (label.Contains("COUCH")) return "couch";
            if (label.Contains("BED")) return "bed";
            if (label.Contains("WALL_FACE") && !label.Contains("INVISIBLE")) return "wall";
            if (label.Contains("DOOR")) return "door";
            if (label.Contains("WINDOW")) return "window";
            return null;
        }
#endif

        RoomGraph BuildEditorFallback()
        {
            _anchors.Clear();
            if (editorAnchors != null)
                foreach (var t in editorAnchors)
                    if (t != null)
                        _anchors[t.name] = t;
            return new RoomGraph
            {
                room_id = "editor_room",
                scanned_at = DateTime.UtcNow.ToString("o"),
                bounds = new RoomBounds { width_m = 4.2f, depth_m = 3.6f, height_m = 2.5f },
                surfaces = new List<RoomSurface>
                {
                    new RoomSurface { id = "desk_a", kind = "desk", area_m2 = 1.2f, height_m = 0.75f, anchor_id = "a1", safe = true },
                    new RoomSurface { id = "table_a", kind = "table", area_m2 = 1.8f, height_m = 0.72f, anchor_id = "a2", safe = true },
                    new RoomSurface { id = "shelf_a", kind = "shelf", area_m2 = 0.4f, height_m = 1.4f, anchor_id = "a3", safe = true },
                    new RoomSurface { id = "wall_n", kind = "wall", area_m2 = 8.0f, height_m = 2.5f, anchor_id = "a4", safe = true }
                },
                volumes = new List<RoomVolume>
                {
                    new RoomVolume { id = "open_center", kind = "open", volume_m3 = 12f }
                }
            };
        }
    }

    [Serializable]
    public sealed class RoomGraph
    {
        public string room_id;
        public string scanned_at;
        public RoomBounds bounds;
        public List<RoomSurface> surfaces = new List<RoomSurface>();
        public List<RoomVolume> volumes = new List<RoomVolume>();
    }

    [Serializable]
    public sealed class RoomBounds
    {
        public float width_m;
        public float depth_m;
        public float height_m;
    }

    [Serializable]
    public sealed class RoomSurface
    {
        public string id;
        public string kind;
        public float area_m2;
        public float height_m;
        public string anchor_id;
        public bool safe = true;
    }

    [Serializable]
    public sealed class RoomVolume
    {
        public string id;
        public string kind;
        public float volume_m3;
    }
}
