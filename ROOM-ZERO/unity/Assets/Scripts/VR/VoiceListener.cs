using UnityEngine;
using RoomZero.Net;

namespace RoomZero.VR
{
    public sealed class VoiceListener : MonoBehaviour
    {
        [SerializeField] AgentClient client;
        [SerializeField] string wake = "ZERO";

        public void SubmitTranscript(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            if (!text.ToUpperInvariant().Contains(wake))
                return;
            if (client != null)
                client.SendVoice(text);
        }

        void Update()
        {
            if (Application.isEditor && Input.GetKeyDown(KeyCode.M))
                SubmitTranscript("ZERO, give me a mission.");
        }
    }
}
