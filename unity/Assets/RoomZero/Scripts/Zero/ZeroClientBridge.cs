using UnityEngine;
using RoomZero.App;
using RoomZero.Game;
using RoomZero.Spatial;

namespace RoomZero.Zero
{
    // Glue between the engine and ZERO: scan -> mission request -> run -> report result.
    public sealed class ZeroClientBridge : MonoBehaviour
    {
        [SerializeField] AgentClient client;
        [SerializeField] RoomScanner scanner;
        [SerializeField] MissionRunner runner;
        [SerializeField] MissionStateMachine machine;
        [SerializeField] SafetyGate safety;

        bool _awaitingScan;

        public bool CanRequest => runner == null || runner.State != MissionState.Running;

        void OnEnable()
        {
            if (client != null)
                client.OnSnapshot += HandleSnapshot;
            if (scanner != null)
                scanner.OnScanned += HandleScanned;
            if (runner != null)
                runner.OnMissionEnded += HandleEnded;
        }

        void OnDisable()
        {
            if (client != null)
                client.OnSnapshot -= HandleSnapshot;
            if (scanner != null)
                scanner.OnScanned -= HandleScanned;
            if (runner != null)
                runner.OnMissionEnded -= HandleEnded;
        }

        void Start()
        {
            if (client != null)
                client.Boot();
            if (machine != null)
                machine.Go(ZeroState.ROOM_READY, "boot");
        }

        public void RequestMission()
        {
            if (safety != null && !safety.IsSafe)
                return;
            if (!CanRequest)
                return;
            _awaitingScan = true;
            if (scanner != null)
                scanner.Scan();
            else
                HandleScanned(null);
        }

        // The scan has been queued to the server; the mission request goes right behind it.
        void HandleScanned(RoomGraph graph)
        {
            if (!_awaitingScan)
                return;
            _awaitingScan = false;
            if (client != null)
                client.GiveMission();
        }

        void HandleSnapshot(MissionData mission)
        {
            if (runner == null || mission == null || string.IsNullOrEmpty(mission.id))
                return;
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

        void HandleEnded(bool won)
        {
            if (client != null && runner != null)
                client.SendComplete(runner.Snapshot());
            if (machine != null)
            {
                machine.Go(ZeroState.COMPLETE, won ? "won" : "failed");
                machine.Go(ZeroState.SAVING, "save");
                machine.Go(ZeroState.READY_FOR_NEXT_SESSION, "saved");
            }
        }
    }
}
