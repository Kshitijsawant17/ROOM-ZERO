using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using RoomZero.Game;
using RoomZero.Spatial;

namespace RoomZero.Zero
{
    // HTTP client for the ZERO orchestrator. Requests are queued and sent one at a
    // time so the server sees them in order (scan before mission, mission before live).
    // Only a mission request delivers a mission; other responses only carry lines/adapts.
    public sealed class AgentClient : MonoBehaviour
    {
        [SerializeField] string baseUrl = "http://127.0.0.1:8765";
        [SerializeField] float timeoutSeconds = 4f;

        public event Action<MissionData> OnMission;
        public event Action<string> OnAdapt;
        public event Action<AdaptData> OnAdaptation;
        public event Action<MissionData> OnSnapshot;
        public event Action<DialogueData> OnLine;
        public event Action<string> OnError;

        public bool Busy => _busy;

        readonly Queue<(string body, bool wantsMission)> _queue = new Queue<(string, bool)>();
        bool _busy;

        // Fresh server session (player memory is reloaded from disk, not lost).
        public void Boot()
        {
            Enqueue("{\"type\":\"boot\",\"payload\":{}}", false);
        }

        public void SendVoice(string text)
        {
            Enqueue("{\"type\":\"voice\",\"payload\":{\"text\":\"" + Escape(text) + "\"}}", true);
        }

        public void GiveMission()
        {
            Enqueue("{\"type\":\"give_mission\",\"payload\":{}}", true);
        }

        public void SendRoomScan(RoomGraph graph)
        {
            Enqueue("{\"type\":\"room_scan\",\"payload\":{\"room\":" + JsonUtility.ToJson(graph) + "}}", false);
        }

        public void SendLive(float stuckS, float idleS)
        {
            // Telemetry is disposable: skip it if the line is busy rather than piling up.
            if (_busy || _queue.Count > 0)
                return;
            Enqueue("{\"type\":\"live\",\"payload\":{\"stuck_s\":" + F(stuckS) + ",\"idle_s\":" + F(idleS) + "}}", false);
        }

        public void SendComplete(SessionStats stats)
        {
            Enqueue("{\"type\":\"complete\",\"payload\":{\"won\":" + (stats.won ? "true" : "false") +
                    ",\"failed\":" + (stats.failed ? "true" : "false") +
                    ",\"duration_s\":" + F(stats.duration_s) +
                    ",\"actions_per_min\":" + F(stats.actions_per_min) + "}}", false);
        }

        void Enqueue(string body, bool wantsMission)
        {
            _queue.Enqueue((body, wantsMission));
            if (!_busy)
                StartCoroutine(Drain());
        }

        IEnumerator Drain()
        {
            _busy = true;
            while (_queue.Count > 0)
            {
                var (body, wantsMission) = _queue.Dequeue();
                yield return Post("/event", body, wantsMission);
            }
            _busy = false;
        }

        IEnumerator Post(string path, string body, bool wantsMission)
        {
            using var req = new UnityWebRequest(baseUrl + path, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = Mathf.CeilToInt(timeoutSeconds);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("ROOM//ZERO agent request failed: " + req.error);
                OnError?.Invoke(req.error);
                yield break;
            }

            bool isLive = body.Contains("\"type\":\"live\"");
            var wrapped = JsonUtility.FromJson<EventResponse>(req.downloadHandler.text);
            if (wrapped == null)
                yield break;
            if (!string.IsNullOrEmpty(wrapped.error))
            {
                Debug.LogWarning("ROOM//ZERO agent error: " + wrapped.error);
                OnError?.Invoke(wrapped.error);
            }

            if (wantsMission && string.IsNullOrEmpty(wrapped.error) && wrapped.plan != null && !string.IsNullOrEmpty(wrapped.plan.template_id))
            {
                var mission = new MissionData
                {
                    id = wrapped.plan.template_id,
                    title = wrapped.plan.title,
                    duration_s = wrapped.plan.duration_s,
                    difficulty = wrapped.plan.difficulty,
                    template_id = wrapped.plan.template_id,
                    primary_kind = wrapped.plan.primary_kind,
                    objective = wrapped.briefing != null && wrapped.briefing.Count > 0 ? wrapped.briefing[0].text : wrapped.plan.title,
                    placements = wrapped.puzzles ?? new List<PlacementData>()
                };
                OnMission?.Invoke(mission);
                OnSnapshot?.Invoke(mission);
                if (wrapped.briefing != null)
                    foreach (var line in wrapped.briefing)
                        OnLine?.Invoke(line);
            }
            else if (wrapped.line != null && !string.IsNullOrEmpty(wrapped.line.text) && (!isLive || wrapped.adapted))
            {
                OnLine?.Invoke(wrapped.line);
            }

            if (isLive && wrapped.adapted && wrapped.adapt != null && !string.IsNullOrEmpty(wrapped.adapt.kind))
            {
                OnAdapt?.Invoke(wrapped.adapt.text);
                OnAdaptation?.Invoke(wrapped.adapt);
            }
        }

        static string F(float v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static string Escape(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        [Serializable]
        sealed class EventResponse
        {
            public MissionData plan;
            public List<PlacementData> puzzles;
            public List<DialogueData> briefing;
            public DialogueData line;
            public AdaptData adapt;
            public string error;
            public bool adapted;
        }
    }

    [Serializable]
    public sealed class AdaptData
    {
        public string kind;
        public float magnitude;
        public string text;
        public bool safety_ok = true;
    }

    [Serializable]
    public sealed class DialogueData
    {
        public string speaker;
        public string intent;
        public string text;
        public string state;
    }
}
