using UnityEngine;
using RoomZero.Interaction;

namespace RoomZero.Game
{
    public sealed class KeycardPuzzle : PuzzleBase, IHandInteractable
    {
        [SerializeField] bool held;
        [SerializeField] float holdSeconds = 1.2f;
        float _hold;

        public void BeginHold()
        {
            held = true;
        }

        public void EndHold()
        {
            held = false;
            _hold = 0f;
        }

        public void OnPoke(Vector3 tip) { }
        public void OnPinchStart(Vector3 point) => BeginHold();
        public void OnPinchHold(Vector3 point, Quaternion handRotation) { }
        public void OnPinchEnd() => EndHold();

        void Update()
        {
            if (Solved)
                return;
            if (Application.isEditor && Input.GetKey(KeyCode.K))
                held = true;
            if (!held)
                return;
            _hold += Time.deltaTime;
            if (_hold >= holdSeconds)
                Complete();
        }
    }
}
