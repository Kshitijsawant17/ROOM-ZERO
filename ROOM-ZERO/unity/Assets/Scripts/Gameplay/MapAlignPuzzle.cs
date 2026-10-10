using UnityEngine;

namespace RoomZero.Gameplay
{
    public sealed class MapAlignPuzzle : PuzzleBase
    {
        [SerializeField] float snapDegrees = 12f;
        float _yaw;

        public void Rotate(float delta)
        {
            if (Solved)
                return;
            _yaw = (_yaw + delta) % 360f;
            if (Mathf.Abs(Mathf.DeltaAngle(_yaw, 0f)) <= snapDegrees)
                Complete();
        }

        void Update()
        {
            if (Application.isEditor && Input.GetKey(KeyCode.RightArrow))
                Rotate(8f * Time.deltaTime * 60f);
        }
    }
}
