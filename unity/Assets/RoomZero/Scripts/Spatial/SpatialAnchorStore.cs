using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomZero.Spatial
{
    // Persists hologram positions across sessions. On Quest each key maps to a saved
    // OVRSpatialAnchor UUID; in the editor it falls back to plain world positions.
    public sealed class SpatialAnchorStore : MonoBehaviour
    {
        const string PrefsKey = "roomzero.anchors";

        [Serializable]
        sealed class Entry { public string key; public string uuid; }
        [Serializable]
        sealed class Saved { public List<Entry> items = new List<Entry>(); }

        readonly Dictionary<string, Vector3> _positions = new Dictionary<string, Vector3>();
        readonly Dictionary<string, Transform> _bound = new Dictionary<string, Transform>();
        Saved _saved = new Saved();

        void Awake()
        {
            var json = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(json))
                _saved = JsonUtility.FromJson<Saved>(json) ?? new Saved();
        }

        public void Bind(string anchorId, Vector3 worldPos)
        {
            _positions[anchorId] = worldPos;
        }

        public bool TryGet(string anchorId, out Vector3 worldPos)
        {
            if (_bound.TryGetValue(anchorId, out var t) && t != null)
            {
                worldPos = t.position;
                return true;
            }
            return _positions.TryGetValue(anchorId, out worldPos);
        }

        // Pins a spawned hologram to the real world and saves it for the next session.
        public void Anchor(string key, GameObject hologram)
        {
            _bound[key] = hologram.transform;
#if META_XR
            if (!Application.isEditor)
                SaveAnchor(key, hologram);
#endif
        }

        public bool HasSaved(string key)
        {
            return _saved.items.Exists(e => e.key == key);
        }

#if META_XR
        async void SaveAnchor(string key, GameObject hologram)
        {
            var anchor = hologram.GetComponent<OVRSpatialAnchor>();
            if (anchor == null)
                anchor = hologram.AddComponent<OVRSpatialAnchor>();
            if (!await anchor.WhenLocalizedAsync())
                return;
            var result = await anchor.SaveAnchorAsync();
            if (!result.Success)
            {
                Debug.LogWarning("ROOM//ZERO anchor save failed: " + key);
                return;
            }
            _saved.items.RemoveAll(e => e.key == key);
            _saved.items.Add(new Entry { key = key, uuid = anchor.Uuid.ToString() });
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(_saved));
            PlayerPrefs.Save();
        }

        // Restores a saved anchor onto a freshly instantiated hologram. Returns false if none.
        public async System.Threading.Tasks.Task<bool> Restore(string key, GameObject hologram)
        {
            var entry = _saved.items.Find(e => e.key == key);
            if (entry == null || !Guid.TryParse(entry.uuid, out var uuid))
                return false;
            var unbound = new List<OVRSpatialAnchor.UnboundAnchor>();
            var load = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(new[] { uuid }, unbound);
            if (!load.Success || unbound.Count == 0)
                return false;
            if (!await unbound[0].LocalizeAsync())
                return false;
            var anchor = hologram.AddComponent<OVRSpatialAnchor>();
            unbound[0].BindTo(anchor);
            _bound[key] = hologram.transform;
            return true;
        }
#endif
    }
}
