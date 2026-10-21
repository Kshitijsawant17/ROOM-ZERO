using UnityEngine;
using RoomZero.Spatial;

namespace RoomZero.Audio
{
    // NPC voice that lives on a piece of real furniture (e.g. Iris on the shelf).
    public sealed class SpatialVoice : MonoBehaviour
    {
        [SerializeField] AudioSource source;
        [SerializeField] RoomScanner scanner;
        [SerializeField] string homeAnchorId;
        [SerializeField] Vector3 offset = new Vector3(0f, 0.25f, 0f);

        void Awake()
        {
            if (source == null)
                source = GetComponent<AudioSource>();
            if (source == null)
                return;
            source.spatialBlend = 1f;
            source.spatialize = true; // Meta XR Audio spatializer when selected in Project Settings
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 0.5f;
            source.maxDistance = 8f;
        }

        void OnEnable()
        {
            if (scanner != null)
                scanner.OnScanned += _ => Attach(homeAnchorId);
        }

        public void Attach(string anchorId)
        {
            if (scanner == null || !scanner.TryGetAnchor(anchorId, out var anchor))
                return;
            transform.SetParent(anchor, false);
            transform.localPosition = offset;
        }

        public void Say(AudioClip clip)
        {
            if (source == null || clip == null)
                return;
            source.PlayOneShot(clip);
        }
    }
}
