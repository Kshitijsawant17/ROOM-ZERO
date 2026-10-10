using UnityEngine;
using RoomZero.Agents;

namespace RoomZero.VR
{
    public sealed class WakeWatcher : MonoBehaviour
    {
        [SerializeField] ZeroClientBridge bridge;
        [SerializeField] KeyCode editorWake = KeyCode.M;

        void Update()
        {
            if (!Application.isEditor)
                return;
            if (Input.GetKeyDown(editorWake) && bridge != null)
                bridge.RequestMission();
        }
    }
}
