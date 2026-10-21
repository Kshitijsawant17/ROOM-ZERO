using UnityEngine;

namespace RoomZero.Interaction
{
    // One per hand. Finds the nearest IHandInteractable collider at the fingertip.
    public sealed class HandInteractor : MonoBehaviour
    {
        [SerializeField] HandController hand;
        [SerializeField] float pokeRadius = 0.015f;
        [SerializeField] float pinchRadius = 0.04f;
        [SerializeField] float pokeCooldown = 0.35f;
        [SerializeField] LayerMask mask = ~0;

        readonly Collider[] _hits = new Collider[8];
        IHandInteractable _held;
        IHandInteractable _lastPoked;
        float _pokeTimer;

        void OnEnable()
        {
            if (hand == null) return;
            hand.OnPinch += BeginPinch;
            hand.OnRelease += EndPinch;
        }

        void OnDisable()
        {
            if (hand == null) return;
            hand.OnPinch -= BeginPinch;
            hand.OnRelease -= EndPinch;
        }

        void Update()
        {
            if (hand == null || !hand.IsTracked)
                return;

            _pokeTimer -= Time.deltaTime;
            var poked = Nearest(hand.Tip, pokeRadius);
            if (poked != null && (poked != _lastPoked || _pokeTimer <= 0f))
            {
                poked.OnPoke(hand.Tip);
                _pokeTimer = pokeCooldown;
            }
            _lastPoked = poked;

            if (_held != null)
                _held.OnPinchHold(hand.PinchPoint, hand.transform.rotation);
        }

        void BeginPinch()
        {
            _held = Nearest(hand.PinchPoint, pinchRadius);
            _held?.OnPinchStart(hand.PinchPoint);
        }

        void EndPinch()
        {
            _held?.OnPinchEnd();
            _held = null;
        }

        IHandInteractable Nearest(Vector3 point, float radius)
        {
            int n = Physics.OverlapSphereNonAlloc(point, radius, _hits, mask, QueryTriggerInteraction.Collide);
            IHandInteractable best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var target = _hits[i].GetComponentInParent<IHandInteractable>();
                if (target == null) continue;
                float d = (_hits[i].ClosestPoint(point) - point).sqrMagnitude;
                if (d < bestD) { bestD = d; best = target; }
            }
            return best;
        }
    }
}
