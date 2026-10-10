using System.Collections.Generic;
using UnityEngine;

namespace RoomZero.Spatial
{
    public sealed class SpatialAnchorStore : MonoBehaviour
    {
        readonly Dictionary<string, Vector3> _anchors = new Dictionary<string, Vector3>();

        public void Bind(string anchorId, Vector3 worldPos)
        {
            _anchors[anchorId] = worldPos;
        }

        public bool TryGet(string anchorId, out Vector3 worldPos)
        {
            return _anchors.TryGetValue(anchorId, out worldPos);
        }
    }
}
