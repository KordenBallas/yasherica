using System;
using System.Collections;
using System.Collections.Generic;
using Inventory.Core;
using Inventory.Data.Definitions;
using UnityEngine;
using Zenject;

namespace Inventory.View
{
    /// <summary>
    /// World-space adapter for the magic pot in the beast's belly: keeps one
    /// bubble per artifact on its assigned spot (diffing by instance id — Track
    /// F), glides survivors down when a removal settles the stack, plays the
    /// drop-in splash and removal bob as spring offsets that settle back to the
    /// rest spots, drifts idle bubbles gently, and re-raises bubble clicks.
    /// </summary>
    public class PotView : MonoBehaviour, IPotView
    {
        [Header("Scene References")]
        [SerializeField] private Transform _bubbleContainer;
        [Tooltip("Looping steam above the liquid; emits only while the pot is in focus")]
        [SerializeField] private ParticleSystem _steamEmitter;

        [Header("Prefabs")]
        [SerializeField] private BubbleView _bubblePrefab;

        [Inject] private InventoryConfig _config;

        private readonly Dictionary<int, SpawnedBubble> _spawnedBubbles =
            new Dictionary<int, SpawnedBubble>();
        private readonly List<int> _removedIds = new List<int>();
        private Coroutine _animateCoroutine;
        private bool _isFocused;

        public event Action<int> OnBubbleClicked;

        private sealed class SpawnedBubble
        {
            public BubbleView View;
            public Vector3 BasePosition;
            public float BobPhase;
            // Spring state for the event physics: a splash kicks Velocity, the
            // integrator settles Offset back to zero (the deterministic rest spot).
            public Vector3 Offset;
            public Vector3 Velocity;
            public bool IsDroppingIn;
        }

        private void OnEnable()
        {
            _animateCoroutine = StartCoroutine(AnimateBubbles());
        }

        private void OnDisable()
        {
            if (_animateCoroutine != null)
            {
                StopCoroutine(_animateCoroutine);
                _animateCoroutine = null;
            }
        }

        public void ShowBubbles(IReadOnlyList<BubbleViewData> bubbles)
        {
            var seen = new HashSet<int>();
            foreach (var data in bubbles)
            {
                seen.Add(data.InstanceId);
                var basePosition = new Vector3(data.Placement.X, data.Placement.Y, data.Placement.Z);
                if (_spawnedBubbles.TryGetValue(data.InstanceId, out var existing))
                {
                    // Settling: when the stack compacts after a removal, the
                    // bubble glides to its lower spot — the spring offset starts
                    // at the old position so nothing teleports. A bubble mid
                    // drop-in just retargets its fall.
                    if (!existing.IsDroppingIn
                        && (existing.BasePosition - basePosition).sqrMagnitude > 1e-8f)
                    {
                        existing.Offset += existing.BasePosition - basePosition;
                    }

                    existing.BasePosition = basePosition;
                    continue;
                }

                SpawnBubble(data, basePosition);
            }

            _removedIds.Clear();
            foreach (var pair in _spawnedBubbles)
            {
                if (!seen.Contains(pair.Key))
                {
                    _removedIds.Add(pair.Key);
                }
            }

            foreach (int instanceId in _removedIds)
            {
                RemoveBubble(instanceId);
            }
        }

        public void SetPotFocused(bool focused)
        {
            _isFocused = focused;
            foreach (var bubble in _spawnedBubbles.Values)
            {
                bubble.View.SetInteractable(focused && !bubble.IsDroppingIn);
            }

            if (_steamEmitter != null)
            {
                if (focused)
                {
                    _steamEmitter.Play();
                }
                else
                {
                    _steamEmitter.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void SpawnBubble(in BubbleViewData data, Vector3 basePosition)
        {
            var view = Instantiate(_bubblePrefab, _bubbleContainer);
            view.transform.localScale = Vector3.one * (data.Placement.Radius * 2f);
            view.Configure(data.InstanceId, data.Icon, data.Tint, GetDepthSettings());
            view.OnClicked += HandleBubbleClicked;

            var bubble = new SpawnedBubble
            {
                View = view,
                BasePosition = basePosition,
                BobPhase = data.Placement.BobPhase
            };
            _spawnedBubbles.Add(data.InstanceId, bubble);

            // While the view is unfocused (initial open build) bubbles appear in
            // place; during a session an arriving artifact falls in and splashes.
            if (_isFocused)
            {
                bubble.IsDroppingIn = true;
                view.SetInteractable(false);
                view.transform.localPosition =
                    basePosition + Vector3.up * (_config != null ? _config.BubbleDropInHeight : 0f);
                StartCoroutine(DropIn(bubble));
            }
            else
            {
                view.transform.localPosition = basePosition;
                view.SetInteractable(false);
            }
        }

        private void RemoveBubble(int instanceId)
        {
            var bubble = _spawnedBubbles[instanceId];
            _spawnedBubbles.Remove(instanceId);

            if (_isFocused && _config != null)
            {
                // Neighbours give a small settle bob but keep their spots.
                Splash(bubble.BasePosition, _config.RemovalBobImpulse);
            }

            if (bubble.View != null)
            {
                bubble.View.OnClicked -= HandleBubbleClicked;
                Destroy(bubble.View.gameObject);
            }
        }

        private IEnumerator DropIn(SpawnedBubble bubble)
        {
            float duration = _config != null ? _config.BubbleDropInDuration : 0f;
            Vector3 start = bubble.View != null
                ? bubble.View.transform.localPosition
                : bubble.BasePosition;

            float elapsed = 0f;
            while (elapsed < duration && bubble.View != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease-in reads as falling: slow release, fast entry into the brew.
                t *= t;
                bubble.View.transform.localPosition = Vector3.Lerp(start, bubble.BasePosition, t);
                yield return null;
            }

            bubble.IsDroppingIn = false;
            if (bubble.View != null)
            {
                bubble.View.transform.localPosition = bubble.BasePosition;
                bubble.View.SetInteractable(_isFocused);
            }

            if (_config != null)
            {
                Splash(bubble.BasePosition, _config.SplashImpulse);
            }
        }

        private void Splash(Vector3 center, float impulse)
        {
            float radius = _config != null ? _config.SplashRadius : 0f;
            if (radius <= 0f || impulse <= 0f)
            {
                return;
            }

            foreach (var bubble in _spawnedBubbles.Values)
            {
                Vector3 away = bubble.BasePosition - center;
                float distance = away.magnitude;
                if (distance <= 0.0001f || distance > radius)
                {
                    continue;
                }

                float falloff = 1f - distance / radius;
                bubble.Velocity += away / distance * (impulse * falloff);
            }
        }

        private ArtifactDepthSettings GetDepthSettings()
        {
            return _config != null
                ? new ArtifactDepthSettings(
                    _config.ArtifactLayerCount,
                    _config.ArtifactLayerSpacing,
                    _config.ArtifactBackLayerDarkening)
                : new ArtifactDepthSettings(1, 0f, 1f);
        }

        private void HandleBubbleClicked(int instanceId)
        {
            OnBubbleClicked?.Invoke(instanceId);
        }

        private IEnumerator AnimateBubbles()
        {
            while (true)
            {
                var driftSettings = new BubbleDriftSettings(
                    _config != null ? _config.BubbleDriftAmplitude : 0f,
                    _config != null ? _config.BubbleDriftFrequency : 0f);
                float stiffness = _config != null ? _config.SettleStiffness : 0f;
                float damping = _config != null ? _config.SettleDamping : 0f;
                float deltaTime = Time.deltaTime;

                foreach (var bubble in _spawnedBubbles.Values)
                {
                    // A bubble mid-drag follows the pointer (drag router); one
                    // mid-drop-in is driven by its own coroutine.
                    if (bubble.View == null || bubble.View.IsDragged || bubble.IsDroppingIn)
                    {
                        continue;
                    }

                    // Damped spring: splash kicks settle back to the rest spot,
                    // so all motion is event-scoped, never perpetual.
                    bubble.Velocity += (-stiffness * bubble.Offset - damping * bubble.Velocity) * deltaTime;
                    bubble.Offset += bubble.Velocity * deltaTime;

                    BubbleDriftCalculator.SampleOffset(
                        Time.time, bubble.BobPhase, driftSettings, out float driftX, out float driftY);
                    bubble.View.transform.localPosition =
                        bubble.BasePosition + bubble.Offset + new Vector3(driftX, driftY, 0f);
                }

                yield return null;
            }
        }
    }
}
