using UnityEngine;

namespace RoomZero.Spatial
{
    // Real room behind the holograms: underlay passthrough + transparent camera clear.
    public sealed class PassthroughRig : MonoBehaviour
    {
        [SerializeField] Camera centerEye;
        [SerializeField] bool enablePassthrough = true;

        void Start()
        {
            if (centerEye == null)
                centerEye = Camera.main;
            if (centerEye == null || !enablePassthrough)
                return;

            centerEye.clearFlags = CameraClearFlags.SolidColor;
            centerEye.backgroundColor = Color.clear;

#if META_XR
            if (OVRManager.instance != null)
                OVRManager.instance.isInsightPassthroughEnabled = true;
            var layer = GetComponent<OVRPassthroughLayer>();
            if (layer == null)
                layer = gameObject.AddComponent<OVRPassthroughLayer>();
            layer.textureOpacity = 1f;
            layer.hidden = false;
#endif
        }
    }
}
