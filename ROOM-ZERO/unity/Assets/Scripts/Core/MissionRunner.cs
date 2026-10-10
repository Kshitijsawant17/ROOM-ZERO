using System;
using System.Collections.Generic;
using UnityEngine;
using RoomZero.Gameplay;
using RoomZero.Spatial;

namespace RoomZero.Core
{
    public sealed class MissionRunner : MonoBehaviour
    {
        [SerializeField] HologramSpawner spawner;
        MissionState _state = MissionState.Idle;
        MissionData _mission;
        float _timer;
        float _stuck;
        float _idle;
        int _actions;
        readonly HashSet<string> _solved = new HashSet<string>();

        public MissionState State => _state;
        public MissionData Mission => _mission;
        public float Timer => _timer;
        public float Stuck => _stuck;
        public int SolvedCount => _solved.Count;
        public event Action<MissionData> OnMissionStarted;
        public event Action<bool> OnMissionEnded;
        public event Action<string> OnBeat;

        public void StartMission(MissionData mission)
        {
            _mission = mission;
            _solved.Clear();
            _timer = mission.duration_s;
            _stuck = 0f;
            _idle = 0f;
            _actions = 0;
            _state = MissionState.Running;
            if (spawner != null)
                spawner.Spawn(mission);
            OnMissionStarted?.Invoke(mission);
            OnBeat?.Invoke(mission.objective);
        }

        public void Solve(string puzzleId)
        {
            if (_state != MissionState.Running)
                return;
            if (_solved.Contains(puzzleId))
                return;
            if (!CanSolve(puzzleId))
                return;
            _solved.Add(puzzleId);
            _stuck = 0f;
            _actions++;
            if (_mission != null && _solved.Count >= Mathf.Max(1, RequiredCount()))
                End(true);
        }

        public void Fail()
        {
            End(false);
        }

        public void EaseTimer(float seconds)
        {
            _timer += seconds;
        }

        public void RegisterAction()
        {
            _actions++;
            _idle = 0f;
            _stuck = 0f;
        }

        public SessionStats Snapshot()
        {
            return new SessionStats
            {
                mission_id = _mission != null ? _mission.id : "",
                duration_s = _mission != null ? _mission.duration_s - _timer : 0f,
                failed = _state == MissionState.Failed,
                won = _state == MissionState.Won,
                actions_per_min = _timer > 0 ? _actions * 60f / Mathf.Max(1f, _mission.duration_s - _timer) : 0f,
                hint_used = 0
            };
        }

        bool CanSolve(string puzzleId)
        {
            if (_mission == null)
                return true;
            foreach (var p in _mission.placements)
            {
                if (p.puzzle_id != puzzleId)
                    continue;
                if (string.IsNullOrEmpty(p.requires))
                    return true;
                return _solved.Contains(p.requires);
            }
            return true;
        }

        int RequiredCount()
        {
            return _mission != null ? Mathf.Max(1, _mission.placements.Count) : 1;
        }

        void Update()
        {
            if (_state != MissionState.Running)
                return;
            _timer -= Time.deltaTime;
            _stuck += Time.deltaTime;
            _idle += Time.deltaTime;
            if (_timer <= 0f)
                End(false);
        }

        void End(bool success)
        {
            _state = success ? MissionState.Won : MissionState.Failed;
            OnMissionEnded?.Invoke(success);
        }
    }

    public enum MissionState
    {
        Idle,
        Running,
        Won,
        Failed
    }

    [Serializable]
    public sealed class MissionData
    {
        public string id;
        public string title;
        public int seed;
        public int duration_s = 180;
        public string difficulty = "normal";
        public string objective;
        public string primary_surface_id;
        public string primary_kind;
        public string template_id;
        public List<PlacementData> placements = new List<PlacementData>();
    }

    [Serializable]
    public sealed class PlacementData
    {
        public string prefab;
        public string surface_id;
        public string puzzle_id;
        public string puzzle_type;
        public string requires;
        public string target_anchor_id;
    }

    [Serializable]
    public sealed class SessionStats
    {
        public string mission_id;
        public float duration_s;
        public bool failed;
        public bool won;
        public float actions_per_min;
        public int hint_used;
    }
}
