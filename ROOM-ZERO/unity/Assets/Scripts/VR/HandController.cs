using UnityEngine;

namespace RoomZero.VR
{
    public sealed class HandController : MonoBehaviour
    {
        [SerializeField] bool left;
        [SerializeField] float pinchThreshold = 0.7f;

        public bool IsPinching { get; private set; }
        public bool IsPoking { get; private set; }
        public Vector3 Tip { get; private set; }

        public event System.Action OnPinch;
        public event System.Action OnRelease;

        void Update()
        {
            Tip = transform.position;
            bool pinch = ReadPinch();
            if (pinch && !IsPinching)
                OnPinch?.Invoke();
            if (!pinch && IsPinching)
                OnRelease?.Invoke();
            IsPinching = pinch;
            IsPoking = ReadPoke();
        }

        bool ReadPinch()
        {
            // Meta Hand Tracking plug-in fills this at runtime.
            // Skeleton keeps a keyboard stand-in for editor play.
            if (Application.isEditor)
                return Input.GetKey(left ? KeyCode.Q : KeyCode.E);
            return false;
        }

        bool ReadPoke()
        {
            if (Application.isEditor)
                return Input.GetKey(left ? KeyCode.A : KeyCode.D);
            return false;
        }
    }
}
