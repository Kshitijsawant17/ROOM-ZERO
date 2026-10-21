using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace RoomZero.EditorTools
{
    // Adds META_XR / META_MRUK / META_VOICE when the Meta XR packages are imported,
    // so runtime scripts compile with or without the SDK.
    [InitializeOnLoad]
    public static class MetaXRDefines
    {
        static readonly (string define, string type)[] Probes =
        {
            ("META_XR", "OVRManager"),
            ("META_MRUK", "Meta.XR.MRUtilityKit.MRUK"),
            ("META_VOICE", "Oculus.Voice.AppVoiceExperience"),
        };

        static MetaXRDefines()
        {
            Apply(NamedBuildTarget.Android);
            Apply(NamedBuildTarget.Standalone);
        }

        static void Apply(NamedBuildTarget target)
        {
            var current = PlayerSettings.GetScriptingDefineSymbols(target)
                .Split(';').Where(s => s.Length > 0).ToList();
            var next = new List<string>(current);
            foreach (var (define, type) in Probes)
            {
                bool present = TypeExists(type);
                if (present && !next.Contains(define)) next.Add(define);
                if (!present) next.Remove(define);
            }
            if (!next.SequenceEqual(current))
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", next));
        }

        static bool TypeExists(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                if (asm.GetType(fullName, false) != null)
                    return true;
            return false;
        }
    }
}
