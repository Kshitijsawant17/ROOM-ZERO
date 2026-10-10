using UnityEngine;

namespace RoomZero.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] string agentUrl = "http://127.0.0.1:8765";
        [SerializeField] bool handsOnly = true;
        [SerializeField] bool passthrough = true;

        public static GameBootstrap Instance { get; private set; }
        public string AgentUrl => agentUrl;
        public bool HandsOnly => handsOnly;
        public bool Passthrough => passthrough;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
        }
    }
}
