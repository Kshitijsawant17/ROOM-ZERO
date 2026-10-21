using UnityEngine;

namespace RoomZero.Game
{
    public sealed class NpcPresence : MonoBehaviour
    {
        [SerializeField] string npcId = "handler";
        [SerializeField] string displayName = "Iris";
        [TextArea] [SerializeField] string line = "Vault is on the largest flat surface.";

        public string NpcId => npcId;
        public string DisplayName => displayName;
        public string Line => line;

        public void SetLine(string text)
        {
            line = text;
        }
    }
}
