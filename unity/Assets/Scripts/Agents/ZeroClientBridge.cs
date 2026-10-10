using UnityEngine;
using RoomZero.Core;
using RoomZero.Net;
using RoomZero.Spatial;

namespace RoomZero.Agents
{
    public sealed class ZeroClientBridge : MonoBehaviour
    {
        [SerializeField] AgentClient client;
        [SerializeField] RoomScanner scanner;
        [SerializeField] MissionRunner runner;
        [SerializeField] MissionStateMachine machine;

        void Start()
        {
            if (client != null)
                client.OnSnapshot += HandleSnapshot;
            if (machine != null)
                machine.Go(ZeroState.ROOM_READY, "boot");
        }

        public void RequestMission()
        {
            if (scanner != null)
                scanner.Scan();
            if (client != null)
                client.GiveMission();
        }

        void HandleSnapshot(MissionData mission)
        {
            if (runner != null && mission != null && !string.IsNullOrEmpty(mission.id))
            {
                if (machine != null)
                {
                    machine.Go(ZeroState.MISSION_REQUESTED, "player");
                    machine.Go(ZeroState.PLANNING, "plan");
                    machine.Go(ZeroState.VALIDATING, "validate");
                    machine.Go(ZeroState.BRIEFING, "brief");
                    machine.Go(ZeroState.ACTIVE, "active");
                }
                runner.StartMission(mission);
            }
        }
    }
}
