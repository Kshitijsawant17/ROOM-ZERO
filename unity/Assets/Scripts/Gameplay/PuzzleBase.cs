using UnityEngine;
using RoomZero.Core;

namespace RoomZero.Gameplay
{
    public abstract class PuzzleBase : MonoBehaviour
    {
        public string PuzzleId;
        public bool Solved { get; protected set; }

        protected void Complete()
        {
            if (Solved)
                return;
            Solved = true;
            var runner = FindObjectOfType<MissionRunner>();
            if (runner != null)
                runner.Solve(PuzzleId);
        }
    }
}
