using UnityEngine;
using RoomZero.App;

namespace RoomZero.Game
{
    public sealed class AlarmMeter : MonoBehaviour
    {
        [SerializeField] float value;
        [SerializeField] MissionRunner runner;

        public float Value => value;

        public void Add(float amount)
        {
            value = Mathf.Clamp01(value + amount);
            if (value >= 1f && runner != null)
                runner.Fail();
        }

        public void Cool(float amount)
        {
            value = Mathf.Clamp01(value - amount);
        }

        void Update()
        {
            Cool(Time.deltaTime * 0.02f);
        }
    }
}
