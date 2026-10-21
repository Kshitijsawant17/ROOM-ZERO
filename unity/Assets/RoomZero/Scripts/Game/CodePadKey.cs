using UnityEngine;
using RoomZero.Interaction;

namespace RoomZero.Game
{
    // One per hologram key (child of the code pad, with a trigger collider).
    public sealed class CodePadKey : MonoBehaviour, IHandInteractable
    {
        [SerializeField] string digit = "0";
        CodePadPuzzle _pad;

        void Awake()
        {
            _pad = GetComponentInParent<CodePadPuzzle>();
        }

        public void OnPoke(Vector3 tip)
        {
            if (_pad != null && !string.IsNullOrEmpty(digit))
                _pad.Press(digit[0]);
        }

        public void OnPinchStart(Vector3 point) { }
        public void OnPinchHold(Vector3 point, Quaternion handRotation) { }
        public void OnPinchEnd() { }
    }
}
