using UnityEngine;
using RoomZero.App;
using RoomZero.Zero;

namespace RoomZero.Game
{
    // Makes ZERO's adaptation visible in the headset, without a debug HUD:
    // HINT_BEAM points a light beam at the next unsolved puzzle, EASE_TIMER adds time.
    public sealed class AdaptationVisuals : MonoBehaviour
    {
        [SerializeField] AgentClient client;
        [SerializeField] MissionRunner runner;
        [SerializeField] LineRenderer beam;
        [SerializeField] TextMesh caption;
        [SerializeField] float beamSeconds = 6f;
        [SerializeField] float captionSeconds = 4f;

        float _beamTimer;
        float _captionTimer;
        Transform _beamTarget;

        void OnEnable()
        {
            if (client != null)
                client.OnAdaptation += Apply;
        }

        void OnDisable()
        {
            if (client != null)
                client.OnAdaptation -= Apply;
        }

        public void Apply(AdaptData adapt)
        {
            if (adapt == null || !adapt.safety_ok)
                return;
            switch (adapt.kind)
            {
                case "HINT_BEAM":
                    _beamTarget = NextUnsolved();
                    _beamTimer = _beamTarget != null ? beamSeconds : 0f;
                    break;
                case "EASE_TIMER":
                    if (runner != null)
                        runner.EaseTimer(Mathf.Clamp(adapt.magnitude, 5f, 20f));
                    break;
            }
            if (caption != null && !string.IsNullOrEmpty(adapt.text))
            {
                caption.text = adapt.text;
                _captionTimer = captionSeconds;
            }
        }

        void Update()
        {
            _beamTimer -= Time.deltaTime;
            _captionTimer -= Time.deltaTime;
            if (runner != null && runner.State != MissionState.Running)
                _beamTimer = _captionTimer = 0f;

            if (caption != null)
                caption.gameObject.SetActive(_captionTimer > 0f);

            if (beam == null)
                return;
            bool show = _beamTimer > 0f && _beamTarget != null && Camera.main != null;
            beam.enabled = show;
            if (!show)
                return;
            var from = Camera.main.transform.position + Vector3.down * 0.3f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            beam.widthMultiplier = Mathf.Lerp(0.004f, 0.012f, pulse);
            beam.positionCount = 2;
            beam.SetPosition(0, from);
            beam.SetPosition(1, _beamTarget.position);
        }

        static Transform NextUnsolved()
        {
            // Point at what the player can do right now, not at a locked puzzle.
            Transform locked = null;
            foreach (var p in FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None))
            {
                if (p.Solved)
                    continue;
                if (p.Unlocked)
                    return p.transform;
                if (locked == null)
                    locked = p.transform;
            }
            return locked;
        }
    }
}
