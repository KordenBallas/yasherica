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
    /// World-space adapter for the magic pot in the beast's belly: spawns artifact
    /// bubbles in the boiling liquid, drifts them slowly around their spots, and
    /// re-raises bubble clicks.
    /// </summary>
    public class PotView : MonoBehaviour, IPotView
    {
        [Header("Scene References")]
        [SerializeField] private Transform _bubbleContainer;
        [Tooltip("Looping steam above the liquid; emits only while the pot is in focus")]
        [SerializeField] private ParticleSystem _steamEmitter;

        [Header("Prefabs")]
        [SerializeField] private BubbleView _bubblePrefab;

        [Header("Pot Interior")]
        [Tooltip("Half extents of the liquid surface area bubbles may occupy (local units)")]
        [SerializeField] private Vector2 _potInteriorHalfExtents = new Vector2(0.5f, 0.3f);
        [Tooltip("Half depth along local Z bubbles may occupy, so they spread in 3D")]
        [SerializeField] private float _potInteriorHalfDepth = 0.12f;

        [Inject] private InventoryConfig _config;

        private readonly List<SpawnedBubble> _spawnedBubbles = new List<SpawnedBubble>();
        private Coroutine _driftCoroutine;
        private bool _isFocused;

        public event Action<int> OnBubbleClicked;

        public Vector2 PotInteriorHalfExtents => _potInteriorHalfExtents;

        public float PotInteriorHalfDepth => _potInteriorHalfDepth;

        private sealed class SpawnedBubble
        {
            public BubbleView View;
            public Vector3 BasePosition;
            public float BobPhase;
        }

        private void OnEnable()
        {
            _driftCoroutine = StartCoroutine(DriftBubbles());
        }

        private void OnDisable()
        {
            if (_driftCoroutine != null)
            {
                StopCoroutine(_driftCoroutine);
                _driftCoroutine = null;
            }
        }

        public void ShowBubbles(IReadOnlyList<BubbleViewData> bubbles)
        {
            ClearBubbles();

            foreach (var data in bubbles)
            {
                var view = Instantiate(_bubblePrefab, _bubbleContainer);
                var basePosition = new Vector3(data.Placement.X, data.Placement.Y, data.Placement.Z);
                view.transform.localPosition = basePosition;
                view.transform.localScale = Vector3.one * (data.Placement.Radius * 2f);
                view.Configure(data.InstanceId, data.Icon, data.Tint, GetDepthSettings());
                view.SetInteractable(_isFocused);
                view.OnClicked += HandleBubbleClicked;

                _spawnedBubbles.Add(new SpawnedBubble
                {
                    View = view,
                    BasePosition = basePosition,
                    BobPhase = data.Placement.BobPhase
                });
            }
        }

        public void SetPotFocused(bool focused)
        {
            _isFocused = focused;
            foreach (var bubble in _spawnedBubbles)
            {
                bubble.View.SetInteractable(focused);
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

        private void ClearBubbles()
        {
            foreach (var bubble in _spawnedBubbles)
            {
                if (bubble.View != null)
                {
                    bubble.View.OnClicked -= HandleBubbleClicked;
                    Destroy(bubble.View.gameObject);
                }
            }

            _spawnedBubbles.Clear();
        }

        private IEnumerator DriftBubbles()
        {
            while (true)
            {
                var settings = new BubbleDriftSettings(
                    _config != null ? _config.BubbleDriftAmplitude : 0f,
                    _config != null ? _config.BubbleDriftFrequency : 0f);

                foreach (var bubble in _spawnedBubbles)
                {
                    if (bubble.View == null)
                    {
                        continue;
                    }

                    BubbleDriftCalculator.SampleOffset(
                        Time.time, bubble.BobPhase, settings, out float offsetX, out float offsetY);
                    bubble.View.transform.localPosition =
                        bubble.BasePosition + new Vector3(offsetX, offsetY, 0f);
                }

                yield return null;
            }
        }
    }
}
