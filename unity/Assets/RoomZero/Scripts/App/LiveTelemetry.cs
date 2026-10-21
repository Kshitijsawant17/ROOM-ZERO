using UnityEngine;
using RoomZero.Game;
using RoomZero.Zero;

namespace RoomZero.App
{
    public sealed class LiveTelemetry : MonoBehaviour
    {
        [SerializeField] MissionRunner runner;
        [SerializeField] AgentClient client;
        [SerializeField] float interval = 2f;
        float _t;

        void Update()
        {
            if (runner == null || runner.State != MissionState.Running)
                return;
            _t += Time.deltaTime;
            if (_t < interval)
                return;
            _t = 0f;
            if (client != null)
                client.SendLive(runner.Stuck, 0f);
        }
    }
}
