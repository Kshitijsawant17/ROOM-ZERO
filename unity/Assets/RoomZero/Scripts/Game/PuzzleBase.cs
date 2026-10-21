using UnityEngine;

namespace RoomZero.Game
{
    public abstract class PuzzleBase : MonoBehaviour
    {
        static readonly Color Locked = new Color(0.35f, 0.4f, 0.45f, 0.6f);
        static readonly Color Ready = new Color(0.2f, 0.9f, 1f, 0.85f);
        static readonly Color Done = new Color(0.3f, 1f, 0.45f, 0.85f);
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        public string PuzzleId;
        public bool Solved { get; protected set; }

        MissionRunner _runner;
        Renderer[] _renderers;
        MaterialPropertyBlock _block;

        protected MissionRunner Runner
        {
            get
            {
                if (_runner == null)
                    _runner = FindFirstObjectByType<MissionRunner>();
                return _runner;
            }
        }

        // Locked until the puzzle it requires is solved.
        public bool Unlocked => Runner == null || Runner.CanSolve(PuzzleId);

        protected virtual void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        protected void Complete()
        {
            if (Solved)
                return;
            // Only lock in the solve if the runner accepts it (respects puzzle order).
            if (Runner == null || Runner.Solve(PuzzleId))
                Solved = true;
        }

        protected virtual void LateUpdate()
        {
            if (_renderers == null)
                return;
            var c = Solved ? Done : Unlocked ? Ready : Locked;
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(ColorId, c);
                _block.SetColor(LegacyColorId, c);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
