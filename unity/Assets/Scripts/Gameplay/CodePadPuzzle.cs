using UnityEngine;

namespace RoomZero.Gameplay
{
    public sealed class CodePadPuzzle : PuzzleBase
    {
        [SerializeField] string code = "2046";
        string _buffer = "";

        public void Press(char digit)
        {
            if (Solved)
                return;
            _buffer += digit;
            if (_buffer.Length < code.Length)
                return;
            if (_buffer == code)
                Complete();
            else
                _buffer = "";
        }

        void Update()
        {
            if (!Application.isEditor)
                return;
            if (Input.GetKeyDown(KeyCode.Alpha2)) Press('2');
            if (Input.GetKeyDown(KeyCode.Alpha0)) Press('0');
            if (Input.GetKeyDown(KeyCode.Alpha4)) Press('4');
            if (Input.GetKeyDown(KeyCode.Alpha6)) Press('6');
        }
    }
}
