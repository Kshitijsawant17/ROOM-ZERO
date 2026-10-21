using UnityEngine;
using RoomZero.App;
using RoomZero.Game;

namespace RoomZero.Spatial
{
    // Places each puzzle on the real furniture the agents chose (target_anchor_id),
    // rejects unsafe surfaces, and pins holograms with spatial anchors.
    public sealed class HologramSpawner : MonoBehaviour
    {
        [SerializeField] GameObject codePadPrefab;
        [SerializeField] GameObject wirePrefab;
        [SerializeField] GameObject mapPrefab;
        [SerializeField] GameObject keycardPrefab;
        [SerializeField] GameObject circuitPrefab;
        [SerializeField] GameObject tokenPrefab;
        [SerializeField] Transform fallbackOrigin;
        [SerializeField] RoomScanner scanner;
        [SerializeField] SpatialAnchorStore anchors;
        [SerializeField] float surfaceOffset = 0.12f;
        [SerializeField] float spawnSeconds = 0.6f;

        public void Spawn(MissionData mission)
        {
            Clear();
            if (mission == null)
                return;
            var origin = fallbackOrigin != null ? fallbackOrigin.position : Vector3.zero;
            for (int i = 0; i < mission.placements.Count; i++)
            {
                var p = mission.placements[i];
                var prefab = Resolve(p.prefab, p.puzzle_type);
                if (prefab == null)
                    continue;
                if (!IsSafe(p.target_anchor_id))
                {
                    Debug.LogWarning("ROOM//ZERO rejected unsafe placement: " + p.target_anchor_id);
                    continue;
                }

                Vector3 pos = origin + Vector3.right * i * 0.4f + Vector3.up * 0.9f;
                Quaternion rot = Quaternion.identity;
                if (scanner != null && scanner.TryGetAnchor(p.target_anchor_id, out var anchor))
                {
                    pos = anchor.position + Vector3.up * surfaceOffset + Spread(i);
                    rot = FaceViewer(pos);
                }

                var go = Instantiate(prefab, pos, rot, transform);
                go.name = p.prefab + "_" + p.target_anchor_id;
                var puzzle = go.GetComponent<PuzzleBase>();
                if (puzzle != null)
                    puzzle.PuzzleId = p.puzzle_id;
                StartCoroutine(Grow(go.transform));
                if (anchors != null)
                    anchors.Anchor(mission.id + "/" + p.puzzle_id, go);
            }
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        bool IsSafe(string anchorId)
        {
            if (scanner == null || scanner.LastGraph == null || string.IsNullOrEmpty(anchorId))
                return true;
            var s = scanner.LastGraph.surfaces.Find(x => x.anchor_id == anchorId);
            return s == null || s.safe;
        }

        static Vector3 Spread(int i)
        {
            // Keep several puzzles on one desk from overlapping.
            return new Vector3((i % 3 - 1) * 0.25f, 0f, 0f);
        }

        static Quaternion FaceViewer(Vector3 pos)
        {
            var cam = Camera.main;
            if (cam == null)
                return Quaternion.identity;
            var dir = pos - cam.transform.position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : Quaternion.identity;
        }

        System.Collections.IEnumerator Grow(Transform t)
        {
            var target = t.localScale;
            for (float e = 0f; e < spawnSeconds && t != null; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, e / spawnSeconds);
                t.localScale = target * k;
                yield return null;
            }
            if (t != null)
                t.localScale = target;
        }

        GameObject Resolve(string name, string type)
        {
            switch (name)
            {
                case "HoloCodePad": return codePadPrefab;
                case "HoloWire": return wirePrefab;
                case "HoloMap": return mapPrefab;
                case "HoloKeycard": return Or(keycardPrefab);
                case "HoloCircuit": return Or(circuitPrefab);
            }
            switch (type)
            {
                case "keycard": return Or(keycardPrefab);
                case "circuit": return Or(circuitPrefab);
            }
            return tokenPrefab;
        }

        GameObject Or(GameObject prefab)
        {
            return prefab != null ? prefab : tokenPrefab;
        }
    }
}
