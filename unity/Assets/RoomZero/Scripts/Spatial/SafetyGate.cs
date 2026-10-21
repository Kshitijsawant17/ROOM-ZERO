using UnityEngine;

namespace RoomZero.Spatial
{
    // Blocks mission start until the player has a Guardian/boundary set up and is
    // using hands (controllers are not allowed in the demo build).
    public sealed class SafetyGate : MonoBehaviour
    {
        [SerializeField] bool requireBoundary = true;
        [SerializeField] bool handsOnly = true;
        [SerializeField] TextMesh warning;

        public bool IsSafe { get; private set; } = true;

        void Update()
        {
            string reason = Check();
            IsSafe = reason == null;
            if (warning != null)
            {
                warning.gameObject.SetActive(!IsSafe);
                if (!IsSafe)
                    warning.text = reason;
            }
        }

        string Check()
        {
#if META_XR
            if (Application.isEditor)
                return null;
            if (requireBoundary && OVRManager.boundary != null && !OVRManager.boundary.GetConfigured())
                return "Set up your Guardian boundary to play.";
            if (handsOnly)
            {
                var active = OVRInput.GetActiveController();
                bool hands = (active & OVRInput.Controller.Hands) != 0;
                if (!hands && active != OVRInput.Controller.None)
                    return "Put controllers down. ROOM//ZERO is hands only.";
            }
#endif
            return null;
        }
    }
}
