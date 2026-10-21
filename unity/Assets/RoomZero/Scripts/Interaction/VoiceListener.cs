using System;
using UnityEngine;
#if META_VOICE
using Oculus.Voice;
#endif

namespace RoomZero.Interaction
{
    // Always-on listening via Meta Voice SDK. Anything that does not contain the
    // wake word "ZERO" is ignored; the rest goes to the agents.
    public sealed class VoiceListener : MonoBehaviour
    {
        [SerializeField] string wake = "ZERO";
        [SerializeField] bool alwaysListen = true;
#if META_VOICE
        [SerializeField] AppVoiceExperience voice;
#endif

        public event Action<string> OnWake;

        void OnEnable()
        {
#if META_VOICE
            if (voice == null)
                return;
            voice.VoiceEvents.OnFullTranscription.AddListener(SubmitTranscript);
            voice.VoiceEvents.OnStoppedListening.AddListener(Relisten);
            if (alwaysListen)
                voice.Activate();
#endif
        }

        void OnDisable()
        {
#if META_VOICE
            if (voice == null)
                return;
            voice.VoiceEvents.OnFullTranscription.RemoveListener(SubmitTranscript);
            voice.VoiceEvents.OnStoppedListening.RemoveListener(Relisten);
            voice.Deactivate();
#endif
        }

#if META_VOICE
        void Relisten()
        {
            if (alwaysListen && isActiveAndEnabled && !voice.Active)
                voice.Activate();
        }
#endif

        public void SubmitTranscript(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            if (!text.ToUpperInvariant().Contains(wake))
                return;
            OnWake?.Invoke(text);
        }

        void Update()
        {
            if (Application.isEditor && Input.GetKeyDown(KeyCode.M))
                SubmitTranscript("ZERO, give me a mission.");
        }
    }
}
