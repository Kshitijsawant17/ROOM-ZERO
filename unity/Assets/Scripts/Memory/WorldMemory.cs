using System;
using System.IO;
using UnityEngine;

namespace RoomZero.Memory
{
    [Serializable]
    public sealed class WorldFlags
    {
        public bool met_iris;
        public string last_primary_kind = "";
    }

    public sealed class WorldMemory : MonoBehaviour
    {
        public WorldFlags Flags { get; private set; } = new WorldFlags();

        string Path => System.IO.Path.Combine(Application.persistentDataPath, "world.json");

        void Awake()
        {
            Load();
        }

        public void Load()
        {
            if (!File.Exists(Path))
                return;
            Flags = JsonUtility.FromJson<WorldFlags>(File.ReadAllText(Path)) ?? new WorldFlags();
        }

        public void Save()
        {
            File.WriteAllText(Path, JsonUtility.ToJson(Flags, true));
        }

        public void RememberVault(string kind)
        {
            Flags.last_primary_kind = kind;
            Flags.met_iris = true;
            Save();
        }
    }
}
