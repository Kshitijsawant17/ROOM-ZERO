using UnityEngine;
using RoomZero.Interaction;

namespace RoomZero.Game
{
    public sealed class MapAlignPuzzle : PuzzleBase, IHandInteractable
    {
        [SerializeField] float snapDegrees = 12f;
        [SerializeField] float startYaw = 135f;
        float _yaw;
        float _lastHandYaw;
        bool _grabbed;

        void Start()
        {
            _yaw = startYaw;
            ApplyYaw();
        }

        public void Rotate(float delta)
        {
            if (Solved)
                return;
            _yaw = (_yaw + delta) % 360f;
            ApplyYaw();
            if (Mathf.Abs(Mathf.DeltaAngle(_yaw, 0f)) <= snapDegrees)
                Complete();
        }

        void ApplyYaw()
        {
            transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        public void OnPoke(Vector3 tip) { }

        public void OnPinchStart(Vector3 point)
        {
            _grabbed = true;
            _lastHandYaw = float.NaN;
        }

        public void OnPinchHold(Vector3 point, Quaternion handRotation)
        {
            if (!_grabbed)
                return;
            float yaw = handRotation.eulerAngles.y;
            if (!float.IsNaN(_lastHandYaw))
                Rotate(Mathf.DeltaAngle(_lastHandYaw, yaw));
            _lastHandYaw = yaw;
        }

        public void OnPinchEnd()
        {
            _grabbed = false;
        }

        void Update()
        {
            if (Application.isEditor && Input.GetKey(KeyCode.RightArrow))
                Rotate(8f * Time.deltaTime * 60f);
        }
    }
}
