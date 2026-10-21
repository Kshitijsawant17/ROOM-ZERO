using UnityEngine;
using RoomZero.Interaction;

namespace RoomZero.Game
{
    public sealed class CircuitPuzzle : PuzzleBase, IHandInteractable
    {
        [SerializeField] int nodes = 4;
        int _lit;

        public void TapNode()
        {
            if (Solved)
                return;
            _lit++;
            if (_lit >= nodes)
                Complete();
        }

        public void OnPoke(Vector3 tip) => TapNode();
        public void OnPinchStart(Vector3 point) { }
        public void OnPinchHold(Vector3 point, Quaternion handRotation) { }
        public void OnPinchEnd() { }

        void Update()
        {
            if (Application.isEditor && Input.GetKeyDown(KeyCode.C))
                TapNode();
        }
    }
}
