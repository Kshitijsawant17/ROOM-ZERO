using UnityEngine;
using RoomZero.Core;

namespace RoomZero.UI
{
    public sealed class MissionHud : MonoBehaviour
    {
        [SerializeField] MissionRunner runner;
        [SerializeField] TextMesh label;

        void LateUpdate()
        {
            if (label == null || runner == null)
                return;
            if (runner.Mission == null)
            {
                label.text = "ZERO is listening";
                return;
            }
            label.text = runner.Mission.title + "\n" + runner.State;
        }
    }
}
