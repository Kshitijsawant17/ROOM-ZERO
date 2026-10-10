using UnityEngine;
using RoomZero.Core;
using RoomZero.Gameplay;

namespace RoomZero.Spatial
{
    public sealed class HologramSpawner : MonoBehaviour
    {
        [SerializeField] GameObject codePadPrefab;
        [SerializeField] GameObject wirePrefab;
        [SerializeField] GameObject mapPrefab;
        [SerializeField] GameObject tokenPrefab;
        [SerializeField] Transform fallbackOrigin;

        public void Spawn(MissionData mission)
        {
            Clear();
            if (mission == null)
                return;
            var origin = fallbackOrigin != null ? fallbackOrigin.position : Vector3.zero;
            for (int i = 0; i < mission.placements.Count; i++)
            {
                var p = mission.placements[i];
                var prefab = Resolve(p.prefab);
                if (prefab == null)
                    continue;
                var go = Instantiate(prefab, origin + Vector3.right * i * 0.4f + Vector3.up * 0.9f, Quaternion.identity, transform);
                var puzzle = go.GetComponent<PuzzleBase>();
                if (puzzle != null)
                    puzzle.PuzzleId = p.puzzle_id;
                if (!string.IsNullOrEmpty(p.target_anchor_id))
                    go.name = p.prefab + "_" + p.target_anchor_id;
            }
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        GameObject Resolve(string name)
        {
            switch (name)
            {
                case "HoloCodePad": return codePadPrefab;
                case "HoloWire": return wirePrefab;
                case "HoloMap": return mapPrefab;
                default: return tokenPrefab;
            }
        }
    }
}
