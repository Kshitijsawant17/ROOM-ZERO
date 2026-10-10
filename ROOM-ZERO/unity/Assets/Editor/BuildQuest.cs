using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RoomZero.EditorTools
{
    public static class BuildQuest
    {
        [MenuItem("ROOMZERO/Build Quest APK")]
        public static void Build()
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Bootstrap.unity" },
                locationPathName = "Builds/ROOMZERO.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("ROOM//ZERO build: " + report.summary.result);
        }
    }
}
