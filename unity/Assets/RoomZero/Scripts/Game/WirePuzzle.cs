using UnityEngine;
using RoomZero.Interaction;

namespace RoomZero.Game
{
    public sealed class WirePuzzle : PuzzleBase, IHandInteractable
    {
        [SerializeField] int requiredLinks = 3;
        int _links;

        public void Link()
        {
            if (Solved)
                return;
            _links++;
            if (_links >= requiredLinks)
                Complete();
        }

        public void OnPoke(Vector3 tip) => Link();
        public void OnPinchStart(Vector3 point) { }
        public void OnPinchHold(Vector3 point, Quaternion handRotation) { }
        public void OnPinchEnd() { }

        void Update()
        {
            if (Application.isEditor && Input.GetKeyDown(KeyCode.W))
                Link();
        }
    }
}
