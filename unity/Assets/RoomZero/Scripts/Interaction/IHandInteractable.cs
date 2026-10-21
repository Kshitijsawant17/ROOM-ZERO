using UnityEngine;

namespace RoomZero.Interaction
{
    // Puzzles implement this; HandInteractor routes pinch / poke / grab to them.
    public interface IHandInteractable
    {
        void OnPoke(Vector3 tip);
        void OnPinchStart(Vector3 point);
        void OnPinchHold(Vector3 point, Quaternion handRotation);
        void OnPinchEnd();
    }
}
