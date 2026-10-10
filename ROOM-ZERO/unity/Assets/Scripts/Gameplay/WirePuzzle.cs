using UnityEngine;

namespace RoomZero.Gameplay
{
    public sealed class WirePuzzle : PuzzleBase
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

        void Update()
        {
            if (Application.isEditor && Input.GetKeyDown(KeyCode.W))
                Link();
        }
    }
}
