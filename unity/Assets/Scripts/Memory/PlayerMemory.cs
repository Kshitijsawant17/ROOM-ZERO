using System;
using System.IO;
using UnityEngine;

namespace RoomZero.Memory
{
    [Serializable]
    public sealed class PlayerRecord
    {
        public string id = "local";
        public int sessions;
        public string style = "stealth";
        public float speed = 20f;
        public int fails;
        public string suggested_difficulty = "normal";
    }

    public sealed class PlayerMemory : MonoBehaviour
    {
        public PlayerRecord Record { get; private set; } = new PlayerRecord();

        string Path => System.IO.Path.Combine(Application.persistentDataPath, "player.json");

        void Awake()
        {
            Load();
        }

        public void Load()
        {
            if (!File.Exists(Path))
                return;
            Record = JsonUtility.FromJson<PlayerRecord>(File.ReadAllText(Path)) ?? new PlayerRecord();
        }

        public void Save()
        {
            File.WriteAllText(Path, JsonUtility.ToJson(Record, true));
        }
    }
}
