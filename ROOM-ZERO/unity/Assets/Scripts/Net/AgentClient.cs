using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using RoomZero.Core;
using RoomZero.Spatial;

namespace RoomZero.Net
{
    public sealed class AgentClient : MonoBehaviour
    {
        [SerializeField] string baseUrl = "http://127.0.0.1:8765";
        public event Action<MissionData> OnMission;
        public event Action<string> OnAdapt;

        public event Action<MissionData> OnSnapshot;

        public void SendVoice(string text)
        {
            StartCoroutine(Post("/event", "{\"type\":\"voice\",\"payload\":{\"text\":\"" + Escape(text) + "\"}}"));
        }

        public void GiveMission()
        {
            StartCoroutine(Post("/event", "{\"type\":\"give_mission\",\"payload\":{}}"));
        }

        public void SendRoomScan(RoomGraph graph)
        {
            var json = JsonUtility.ToJson(graph);
            StartCoroutine(Post("/event", "{\"type\":\"room_scan\",\"payload\":{\"room\":" + json + "}}"));
        }

        public void SendLive(float stuckS, float idleS)
        {
            StartCoroutine(Post("/event", "{\"type\":\"live\",\"payload\":{\"stuck_s\":" + stuckS + ",\"idle_s\":" + idleS + "}}"));
        }

        IEnumerator Post(string path, string body)
        {
            var req = new UnityWebRequest(baseUrl + path, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                yield break;
            var wrapped = JsonUtility.FromJson<EventResponse>(req.downloadHandler.text);
            if (wrapped != null && wrapped.plan != null && !string.IsNullOrEmpty(wrapped.plan.template_id))
            {
                var mission = new MissionData
                {
                    id = wrapped.plan.template_id,
                    title = wrapped.plan.title,
                    duration_s = wrapped.plan.duration_s,
                    difficulty = wrapped.plan.difficulty,
                    template_id = wrapped.plan.template_id,
                    primary_kind = wrapped.plan.primary_kind,
                    placements = wrapped.puzzles ?? new System.Collections.Generic.List<PlacementData>()
                };
                OnMission?.Invoke(mission);
                OnSnapshot?.Invoke(mission);
            }
        }

        static string Escape(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        [Serializable]
        sealed class EventResponse
        {
            public MissionData mission;
            public MissionData plan;
            public System.Collections.Generic.List<PlacementData> puzzles;
        }
    }
}
