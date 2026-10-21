using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RoomZero.Game;
using RoomZero.Zero;
using Object = UnityEngine.Object;

namespace RoomZero.EditorTools
{
    // End-to-end check against a running orchestrator:
    //   Unity -batchmode -projectPath unity -executeMethod RoomZero.EditorTools.SmokeTest.Run [-shots <dir>]
    // Plays Bootstrap.unity, requests a mission, waits for ZERO to adapt, solves every
    // puzzle in order and expects a win. With -shots, saves a screenshot per step.
    public static class SmokeTest
    {
        const float TimeoutS = 90f;
        const float StuckWaitS = 28f;

        enum Phase { Boot, Requested, WaitHint, Solving, Done }

        static Phase _phase;
        static float _start, _phaseAt;
        static string _shots;
        static string _mission = "";
        static bool _hinted;
        static int _solvedShots;
        static readonly HashSet<PuzzleBase> _holding = new HashSet<PuzzleBase>();
        static readonly List<string> _lines = new List<string>();

        public static void Run()
        {
            _shots = Arg("-shots");
            if (_shots != null)
                Directory.CreateDirectory(_shots);
            EditorSceneManager.OpenScene("Assets/RoomZero/Scenes/Bootstrap.unity");
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            EditorApplication.playModeStateChanged += OnState;
            EditorApplication.EnterPlaymode();
        }

        static void OnState(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode)
                return;
            _start = _phaseAt = Time.realtimeSinceStartup;
            var client = Object.FindFirstObjectByType<AgentClient>();
            client.OnLine += l => { _lines.Add(RoomZero.UI.MissionHud.SpeakerLine(l)); Debug.Log("SMOKE line: " + RoomZero.UI.MissionHud.SpeakerLine(l)); };
            client.OnError += e => Debug.Log("SMOKE agent error: " + e);
            client.OnAdaptation += a => { _hinted = true; Debug.Log("SMOKE adapt: " + a.kind + " " + a.text); };
            EditorApplication.update += Tick;
        }

        static void Go(Phase next)
        {
            _phase = next;
            _phaseAt = Time.realtimeSinceStartup;
        }

        static void Tick()
        {
            float now = Time.realtimeSinceStartup;
            float inPhase = now - _phaseAt;
            var runner = Object.FindFirstObjectByType<MissionRunner>();
            var bridge = Object.FindFirstObjectByType<ZeroClientBridge>();
            var client = Object.FindFirstObjectByType<AgentClient>();

            if (now - _start > TimeoutS)
            {
                Finish(false, "timeout in " + _phase + "; state=" + runner.State + " mission=" + _mission);
                return;
            }

            switch (_phase)
            {
                case Phase.Boot:
                    if (inPhase < 1.5f || client.Busy)
                        return;
                    Shot("01_room", "Empty room. ZERO is listening.");
                    Debug.Log("SMOKE: requesting mission");
                    bridge.RequestMission();
                    Go(Phase.Requested);
                    return;

                case Phase.Requested:
                    if (runner.Mission == null)
                        return;
                    if (inPhase < 1.5f)
                        return; // let holograms finish their grow-in
                    _mission = runner.Mission.title + " [" + string.Join(", ", runner.Mission.placements.ConvertAll(p => p.puzzle_type + "@" + p.surface_id)) + "]";
                    Debug.Log("SMOKE: mission " + _mission);
                    Shot("02_mission", "Mission: " + runner.Mission.title);
                    Go(_shots != null ? Phase.WaitHint : Phase.Solving);
                    return;

                case Phase.WaitHint:
                    // Do nothing so the player looks stuck; ZERO should adapt after ~25s.
                    if (_hinted && inPhase > 1f)
                    {
                        Shot("03_hint", "ZERO adapts: hint beam");
                        Go(Phase.Solving);
                    }
                    else if (inPhase > StuckWaitS + 10f)
                    {
                        Debug.Log("SMOKE: no adaptation arrived");
                        Go(Phase.Solving);
                    }
                    return;

                case Phase.Solving:
                    if (runner.State == MissionState.Won)
                    {
                        if (client.Busy)
                            return; // let the complete event reach the server
                        Shot("05_won", "Mission complete");
                        Go(Phase.Done);
                        Finish(true, "won " + _mission + (_hinted ? " (adapted)" : ""));
                        return;
                    }
                    if (runner.State == MissionState.Failed)
                    {
                        Finish(false, "mission failed");
                        return;
                    }
                    if (runner.SolvedCount > _solvedShots && runner.SolvedCount < runner.Mission.placements.Count)
                    {
                        _solvedShots = runner.SolvedCount;
                        Shot("04_progress", "First puzzle solved, next one unlocked");
                    }
                    if (inPhase < 0.4f)
                        return;
                    SolveStep();
                    return;
            }
        }

        static void SolveStep()
        {
            foreach (var p in Object.FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None))
            {
                if (p.Solved || !p.Unlocked)
                    continue;
                switch (p)
                {
                    case CodePadPuzzle pad: foreach (var c in "2046") pad.Press(c); break;
                    case WirePuzzle wire: wire.Link(); break;
                    case CircuitPuzzle circuit: circuit.TapNode(); break;
                    case MapAlignPuzzle map: map.Rotate(15f); break;
                    case KeycardPuzzle card:
                        if (_holding.Add(card)) card.BeginHold();
                        break;
                }
            }
        }

        static void Shot(string name, string caption)
        {
            if (_shots == null)
                return;
            var cam = Camera.main;
            var rt = new RenderTexture(1280, 720, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(_shots, name + ".png"), tex.EncodeToJPG(82));
            Object.DestroyImmediate(tex);
            rt.Release();

            var runner = Object.FindFirstObjectByType<MissionRunner>();
            var info = new List<string> { caption, "state=" + runner.State, "timer=" + Mathf.CeilToInt(Mathf.Max(0, runner.Timer)) };
            if (runner.Mission != null)
                info.Add("solved=" + runner.SolvedCount + "/" + runner.Mission.placements.Count);
            if (_lines.Count > 0)
                info.Add("line=" + _lines[_lines.Count - 1]);
            File.WriteAllText(Path.Combine(_shots, name + ".txt"), string.Join("\n", info));
            Debug.Log("SMOKE shot: " + name);
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name)
                    return args[i + 1];
            return null;
        }

        static void Finish(bool ok, string message)
        {
            EditorApplication.update -= Tick;
            Debug.Log((ok ? "SMOKE PASS: " : "SMOKE FAIL: ") + message);
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorApplication.ExitPlaymode();
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
