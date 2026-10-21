using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RoomZero.EditorTools
{
    public static class BuildQuest
    {
        [MenuItem("ROOMZERO/Configure Quest Settings")]
        public static void Configure()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.roomzero.quest");
            PlayerSettings.productName = "ROOM//ZERO";
            // Orchestrator on a companion PC over plain HTTP during the demo.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;

#if META_XR
            var cfg = OVRProjectConfig.CachedProjectConfig;
            cfg.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
            cfg.handTrackingFrequency = OVRProjectConfig.HandTrackingFrequency.HIGH;
            cfg.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
            cfg.sceneSupport = OVRProjectConfig.FeatureSupport.Required;
            cfg.anchorSupport = OVRProjectConfig.AnchorSupport.Enabled;
            OVRProjectConfig.CommitProjectConfig(cfg);
#else
            Debug.LogWarning("ROOM//ZERO: Meta XR SDK not imported yet. Open Package Manager and let com.meta.xr.sdk.all resolve.");
#endif
            Debug.Log("ROOM//ZERO Quest settings applied.");
        }

        [MenuItem("ROOMZERO/Build Quest APK")]
        public static void Build()
        {
            Configure();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/RoomZero/Scenes/Bootstrap.unity" },
                locationPathName = "Builds/ROOMZERO.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("ROOM//ZERO build: " + report.summary.result);
        }
    }
}
