using UnityEngine;

namespace RoomZero.Interaction
{
    // Hands-only input: pinch, poke (index tip) and grab (pinch held on a target).
    public sealed class HandController : MonoBehaviour
    {
        [SerializeField] bool left;
        [SerializeField] float pinchThreshold = 0.7f;
#if META_XR
        [SerializeField] OVRHand hand;
        [SerializeField] OVRSkeleton skeleton;
#endif

        public bool Left => left;
        public bool IsTracked { get; private set; }
        public bool IsPinching { get; private set; }
        public Vector3 Tip { get; private set; }
        public Vector3 PinchPoint { get; private set; }

        public event System.Action OnPinch;
        public event System.Action OnRelease;

        void Update()
        {
            ReadPose();
            bool pinch = ReadPinch();
            if (pinch && !IsPinching)
                OnPinch?.Invoke();
            if (!pinch && IsPinching)
                OnRelease?.Invoke();
            IsPinching = pinch;
        }

        void ReadPose()
        {
#if META_XR
            IsTracked = hand != null && hand.IsTracked && hand.IsDataHighConfidence;
            if (IsTracked && skeleton != null && skeleton.IsDataValid)
            {
                Vector3 index = Tip, thumb = Tip;
                foreach (var bone in skeleton.Bones)
                {
                    if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip) index = bone.Transform.position;
                    else if (bone.Id == OVRSkeleton.BoneId.Hand_ThumbTip) thumb = bone.Transform.position;
                }
                Tip = index;
                PinchPoint = (index + thumb) * 0.5f;
                return;
            }
#endif
            IsTracked = Application.isEditor;
            Tip = transform.position;
            PinchPoint = transform.position;
        }

        bool ReadPinch()
        {
#if META_XR
            if (hand != null && IsTracked)
                return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) >= pinchThreshold;
#endif
            if (Application.isEditor)
                return Input.GetKey(left ? KeyCode.Q : KeyCode.E);
            return false;
        }
    }
}
