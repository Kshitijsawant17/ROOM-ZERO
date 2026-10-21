using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomZero.App
{
    public enum ZeroState
    {
        BOOT,
        ROOM_READY,
        MISSION_REQUESTED,
        PLANNING,
        VALIDATING,
        BRIEFING,
        ACTIVE,
        ADAPTING,
        COMPLETE,
        SAVING,
        READY_FOR_NEXT_SESSION
    }

    public sealed class MissionStateMachine : MonoBehaviour
    {
        public ZeroState State { get; private set; } = ZeroState.BOOT;
        public string LastError { get; private set; }
        public event Action<ZeroState, ZeroState, string> OnTransition;

        static readonly Dictionary<ZeroState, ZeroState[]> Allowed = new Dictionary<ZeroState, ZeroState[]>
        {
            { ZeroState.BOOT, new[] { ZeroState.ROOM_READY } },
            { ZeroState.ROOM_READY, new[] { ZeroState.MISSION_REQUESTED, ZeroState.ROOM_READY } },
            { ZeroState.MISSION_REQUESTED, new[] { ZeroState.PLANNING, ZeroState.ROOM_READY } },
            { ZeroState.PLANNING, new[] { ZeroState.VALIDATING, ZeroState.ROOM_READY } },
            { ZeroState.VALIDATING, new[] { ZeroState.BRIEFING, ZeroState.PLANNING } },
            { ZeroState.BRIEFING, new[] { ZeroState.ACTIVE } },
            { ZeroState.ACTIVE, new[] { ZeroState.ADAPTING, ZeroState.COMPLETE } },
            { ZeroState.ADAPTING, new[] { ZeroState.ACTIVE } },
            { ZeroState.COMPLETE, new[] { ZeroState.SAVING } },
            { ZeroState.SAVING, new[] { ZeroState.READY_FOR_NEXT_SESSION } },
            { ZeroState.READY_FOR_NEXT_SESSION, new[] { ZeroState.MISSION_REQUESTED, ZeroState.ROOM_READY } }
        };

        static readonly Dictionary<ZeroState, float> TimeoutS = new Dictionary<ZeroState, float>
        {
            { ZeroState.BOOT, 3f },
            { ZeroState.ROOM_READY, 30f },
            { ZeroState.MISSION_REQUESTED, 2f },
            { ZeroState.PLANNING, 1.5f },
            { ZeroState.VALIDATING, 0.8f },
            { ZeroState.BRIEFING, 8f },
            { ZeroState.ACTIVE, 240f },
            { ZeroState.ADAPTING, 0.4f },
            { ZeroState.COMPLETE, 2f },
            { ZeroState.SAVING, 1f },
            { ZeroState.READY_FOR_NEXT_SESSION, 0f }
        };

        float _entered;

        void Awake()
        {
            _entered = Time.realtimeSinceStartup;
        }

        public bool CanGo(ZeroState next)
        {
            ZeroState[] allowed;
            return Allowed.TryGetValue(State, out allowed) && Array.IndexOf(allowed, next) >= 0;
        }

        public bool Go(ZeroState next, string reason)
        {
            if (next == State)
            {
                _entered = Time.realtimeSinceStartup;
                return true;
            }
            if (!CanGo(next))
            {
                LastError = State + " -> " + next;
                return false;
            }
            var prev = State;
            State = next;
            _entered = Time.realtimeSinceStartup;
            LastError = null;
            OnTransition?.Invoke(prev, next, reason);
            return true;
        }

        public bool TimedOut()
        {
            float limit;
            if (!TimeoutS.TryGetValue(State, out limit) || limit <= 0f)
                return false;
            return Time.realtimeSinceStartup - _entered > limit;
        }
    }
}
