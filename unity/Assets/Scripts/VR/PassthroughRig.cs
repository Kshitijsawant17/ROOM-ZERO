using UnityEngine;

namespace RoomZero.VR
{
    public sealed class PassthroughRig : MonoBehaviour
    {
        [SerializeField] Camera centerEye;
        [SerializeField] bool enablePassthrough = true;

        void Start()
        {
            if (centerEye == null)
                centerEye = Camera.main;
            if (centerEye != null && enablePassthrough)
                centerEye.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
