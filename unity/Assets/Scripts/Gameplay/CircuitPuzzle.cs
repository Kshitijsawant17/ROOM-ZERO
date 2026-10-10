using UnityEngine;

namespace RoomZero.Gameplay
{
    public sealed class CircuitPuzzle : PuzzleBase
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

        void Update()
        {
            if (Application.isEditor && Input.GetKeyDown(KeyCode.C))
                TapNode();
        }
    }
}
