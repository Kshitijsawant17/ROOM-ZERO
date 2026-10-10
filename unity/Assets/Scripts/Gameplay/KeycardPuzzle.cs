using UnityEngine;

namespace RoomZero.Gameplay
{
    public sealed class KeycardPuzzle : PuzzleBase
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
