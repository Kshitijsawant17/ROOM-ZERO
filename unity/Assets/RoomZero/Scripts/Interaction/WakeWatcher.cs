using UnityEngine;
using RoomZero.Zero;

namespace RoomZero.Interaction
{
    // "ZERO, ..." starts a scan + mission request.
    public sealed class WakeWatcher : MonoBehaviour
    {
        [SerializeField] ZeroClientBridge bridge;
        [SerializeField] VoiceListener voice;

        void OnEnable()
        {
            if (voice != null)
                voice.OnWake += HandleWake;
        }

        void OnDisable()
        {
            if (voice != null)
                voice.OnWake -= HandleWake;
        }

        void HandleWake(string text)
        {
            if (bridge != null)
                bridge.RequestMission();
        }
    }
}
