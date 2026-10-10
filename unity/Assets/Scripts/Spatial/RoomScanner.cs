using System;
using System.Collections.Generic;
using UnityEngine;
using RoomZero.Net;

namespace RoomZero.Spatial
{
    public sealed class RoomScanner : MonoBehaviour
    {
        [SerializeField] AgentClient client;
        public RoomGraph LastGraph { get; private set; }
        public event Action<RoomGraph> OnScanned;

        public void Scan()
        {
            LastGraph = BuildEditorFallback();
            OnScanned?.Invoke(LastGraph);
            if (client != null)
                client.SendRoomScan(LastGraph);
        }

        RoomGraph BuildEditorFallback()
        {
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
