using UnityEngine;
using RoomZero.Game;
using RoomZero.Zero;

namespace RoomZero.UI
{
    // World-space mission label for the headset, plus an editor-only test panel
    // with keyboard controls and the latest ZERO line.
    public sealed class MissionHud : MonoBehaviour
    {
        [SerializeField] MissionRunner runner;
        [SerializeField] AgentClient client;
        [SerializeField] TextMesh label;
        [SerializeField] bool showEditorPanel = true;

        string _line = "";
        string _status = "";

        void OnEnable()
        {
            if (client != null)
            {
                client.OnLine += HandleLine;
                client.OnError += HandleError;
            }
            if (runner != null)
                runner.OnMissionEnded += HandleEnded;
        }

        void OnDisable()
        {
            if (client != null)
            {
                client.OnLine -= HandleLine;
                client.OnError -= HandleError;
            }
            if (runner != null)
                runner.OnMissionEnded -= HandleEnded;
        }

        void HandleLine(DialogueData line) => _line = SpeakerLine(line);

        public static string SpeakerLine(DialogueData line)
        {
            if (string.IsNullOrEmpty(line.speaker) || line.text.StartsWith(line.speaker + ":"))
                return line.text;
            return line.speaker + ": " + line.text;
        }
        void HandleError(string error) => _status = "Agent: " + error;
        void HandleEnded(bool won) => _status = won ? "MISSION COMPLETE. Press M for the next one." : "MISSION FAILED. Press M to retry.";

        void LateUpdate()
        {
            if (label == null || runner == null)
                return;
            if (runner.Mission == null)
            {
                label.text = "ZERO is listening";
                return;
            }
            label.text = runner.Mission.title + "\n" + runner.State + "  " + Mathf.CeilToInt(Mathf.Max(0f, runner.Timer)) + "s";
        }

        void OnGUI()
        {
            if (!showEditorPanel || !Application.isEditor)
                return;
            GUILayout.BeginArea(new Rect(12, 12, 440, 420), GUI.skin.box);
            GUILayout.Label("<b>ROOM//ZERO — editor test</b>", Rich());
            if (runner != null && runner.Mission != null)
            {
                GUILayout.Label("Mission: " + runner.Mission.title + "  (" + runner.Mission.difficulty + ")");
                GUILayout.Label("State: " + runner.State + "   Time: " + Mathf.CeilToInt(Mathf.Max(0f, runner.Timer)) + "s   Solved: " + runner.SolvedCount + "/" + runner.Mission.placements.Count);
                foreach (var p in runner.Mission.placements)
                {
                    string mark = runner.CanSolve(p.puzzle_id) ? "▶" : "🔒";
                    GUILayout.Label("  " + mark + " " + p.puzzle_type + " on " + p.surface_id + "  [" + KeyFor(p) + "]");
                }
            }
            else
            {
                GUILayout.Label("No mission yet.");
            }
            if (!string.IsNullOrEmpty(_line))
                GUILayout.Label(_line, Wrap());
            if (!string.IsNullOrEmpty(_status))
                GUILayout.Label("<b>" + _status + "</b>", Rich());
            GUILayout.Space(6);
            GUILayout.Label("M = \"ZERO, give me a mission\"\n2,0,4,6 = code pad   W = wire   C = circuit\nK (hold) = keycard / token   → (hold) = map\nIdle 25s = ZERO adapts (hint beam / more time)", Wrap());
            GUILayout.EndArea();
        }

        static string KeyFor(PlacementData p)
        {
            switch (p.puzzle_type)
            {
                case "code_pad": return "2046";
                case "wire": return "W x3";
                case "circuit": return "C x4";
                case "map_align": return "hold →";
                default: return "hold K";
            }
        }

        static GUIStyle Rich() => new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        static GUIStyle Wrap() => new GUIStyle(GUI.skin.label) { wordWrap = true };
    }
}
