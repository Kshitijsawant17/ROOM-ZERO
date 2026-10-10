using UnityEngine;

namespace RoomZero.Audio
{
    public sealed class SpatialVoice : MonoBehaviour
    {
        [SerializeField] AudioSource source;

        public void Say(AudioClip clip)
        {
            if (source == null || clip == null)
                return;
            source.spatialBlend = 1f;
            source.PlayOneShot(clip);
        }
    }
}
