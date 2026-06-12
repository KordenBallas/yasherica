using System;
using System.Collections;
using System.Collections.Generic;
using Loot.Data.Definitions;
using UnityEngine;
using Zenject;

namespace Loot.View
{
    /// <summary>
    /// World artifact pickup: a flat icon quad given depth by a rear sprite stack
    /// (BubbleView pattern), a blob shadow beneath, and a subtle idle bob.
    /// Pure adapter - pickup decisions live in WorldArtifactPresenter.
    /// </summary>
    public class WorldArtifactView : MonoBehaviour, IWorldArtifactView
    {
        public class Factory : PlaceholderFactory<WorldArtifactView> { }

        // URP Lit shader properties.
        private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        [Header("Renderers")]
        [SerializeField] private MeshRenderer _iconRenderer;
        [SerializeField] private MeshRenderer _shadowRenderer;

        [Header("Interaction")]
        [SerializeField] private Collider _triggerCollider;

        private readonly List<MeshRenderer> _depthLayers = new List<MeshRenderer>();
        private LootConfig _config;
        private Vector3 _iconRestPosition;
        private Coroutine _bobRoutine;
        private Coroutine _pickupRoutine;
        private Coroutine _rejectRoutine;

        public event Action<Transform> OnBodyEntered;
        public event Action OnViewDestroyed;

        public void Configure(Sprite icon, LootConfig config)
        {
            _config = config;

            if (_iconRenderer != null && icon != null)
            {
                ApplyIconLook(_iconRenderer, icon, Color.white);
                BuildDepthLayers(icon);
            }

            ConfigureShadow();

            _iconRestPosition = _iconRenderer != null
                ? _iconRenderer.transform.localPosition
                : Vector3.zero;
            StartBob();
        }

        public void SetInteractable(bool interactable)
        {
            if (_triggerCollider != null)
            {
                _triggerCollider.enabled = interactable;
            }
        }

        public void PlayPickupAnimation(Transform moveTarget, Action onComplete)
        {
            StopRoutine(ref _bobRoutine);
            StopRoutine(ref _pickupRoutine);
            _pickupRoutine = StartCoroutine(PickupRoutine(moveTarget, onComplete));
        }

        public void PlayRejectFeedback()
        {
            StopRoutine(ref _rejectRoutine);
            _rejectRoutine = StartCoroutine(RejectRoutine());
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            OnBodyEntered?.Invoke(other.transform);
        }

        private void OnDestroy()
        {
            OnViewDestroyed?.Invoke();
        }

        private IEnumerator PickupRoutine(Transform moveTarget, Action onComplete)
        {
            Vector3 startScale = transform.localScale;
            Vector3 startPosition = transform.position;
            float duration = Mathf.Max(0.01f, _config.PickupDuration);
            float peak = _config.PickupPeakFraction;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float progress = elapsed / duration;

                float scale = progress < peak
                    ? Mathf.Lerp(1f, _config.PickupGrowScale, SmoothStep01(progress / peak))
                    : Mathf.Lerp(_config.PickupGrowScale, 0f, SmoothStep01((progress - peak) / (1f - peak)));
                transform.localScale = startScale * scale;

                if (_config.PickupMoveToPlayer && moveTarget != null)
                {
                    transform.position = Vector3.Lerp(startPosition, moveTarget.position, SmoothStep01(progress));
                }

                if (_config.PickupFadeEnabled && progress >= peak)
                {
                    SetAlpha(1f - (progress - peak) / (1f - peak));
                }

                yield return null;
            }

            transform.localScale = Vector3.zero;
            _pickupRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator RejectRoutine()
        {
            // Horizontal shake of the icon around its rest position.
            const float shakeAmplitude = 0.1f;
            const float shakeCycles = 3f;
            float duration = Mathf.Max(0.01f, _config.RejectShakeDuration);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float progress = elapsed / duration;
                float offset = Mathf.Sin(progress * shakeCycles * 2f * Mathf.PI)
                    * shakeAmplitude * (1f - progress);
                SetIconLocalPosition(_iconRestPosition + new Vector3(offset, 0f, 0f));
                yield return null;
            }

            SetIconLocalPosition(_iconRestPosition);
            _rejectRoutine = null;
        }

        private void StartBob()
        {
            StopRoutine(ref _bobRoutine);
            if (_config.BobAmplitude > 0f)
            {
                _bobRoutine = StartCoroutine(BobRoutine());
            }
        }

        private IEnumerator BobRoutine()
        {
            // Phase offset per instance so neighbouring pickups do not bob in sync.
            float phase = (GetInstanceID() & 0xFF) / 255f * 2f * Mathf.PI;
            while (true)
            {
                float offset = Mathf.Sin(Time.time * _config.BobFrequency * 2f * Mathf.PI + phase)
                    * _config.BobAmplitude;
                SetIconLocalPosition(_iconRestPosition + new Vector3(0f, offset, 0f));
                yield return null;
            }
        }

        private void SetIconLocalPosition(Vector3 localPosition)
        {
            if (_iconRenderer != null)
            {
                _iconRenderer.transform.localPosition = localPosition;
            }
        }

        // Rear layers are clones of the front quad parented under it, pushed back
        // along its depth axis and darkened progressively (BubbleView pattern).
        // Parenting under the icon keeps them aligned during bob and pickup.
        private void BuildDepthLayers(Sprite icon)
        {
            foreach (var layer in _depthLayers)
            {
                if (layer != null)
                {
                    Destroy(layer.gameObject);
                }
            }
            _depthLayers.Clear();

            Transform front = _iconRenderer.transform;
            for (int i = 1; i < _config.DepthLayerCount; i++)
            {
                var layer = Instantiate(_iconRenderer, front);
                layer.transform.localRotation = Quaternion.identity;
                layer.transform.localScale = Vector3.one;
                layer.transform.localPosition = Vector3.forward * (_config.DepthLayerSpacing * i);

                float fade = _config.DepthLayerCount > 1 ? i / (float)(_config.DepthLayerCount - 1) : 0f;
                float brightness = Mathf.Lerp(1f, _config.DepthLayerDarkening, fade);
                ApplyIconLook(layer, icon, new Color(brightness, brightness, brightness, 1f));

                _depthLayers.Add(layer);
            }
        }

        private void ConfigureShadow()
        {
            if (_shadowRenderer == null)
            {
                return;
            }

            _shadowRenderer.transform.localPosition = Vector3.down * _config.ShadowVerticalOffset;
            _shadowRenderer.transform.localScale = Vector3.one * _config.ShadowScale;

            var block = new MaterialPropertyBlock();
            _shadowRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorProperty, new Color(0f, 0f, 0f, _config.ShadowAlpha));
            _shadowRenderer.SetPropertyBlock(block);
        }

        private void SetAlpha(float alpha)
        {
            ApplyAlpha(_iconRenderer, alpha);
            foreach (var layer in _depthLayers)
            {
                ApplyAlpha(layer, alpha);
            }

            if (_shadowRenderer != null)
            {
                var block = new MaterialPropertyBlock();
                _shadowRenderer.GetPropertyBlock(block);
                block.SetColor(BaseColorProperty, new Color(0f, 0f, 0f, _config.ShadowAlpha * alpha));
                _shadowRenderer.SetPropertyBlock(block);
            }
        }

        private static void ApplyAlpha(MeshRenderer renderer, float alpha)
        {
            if (renderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var color = block.GetColor(BaseColorProperty);
            color.a = Mathf.Clamp01(alpha);
            block.SetColor(BaseColorProperty, color);
            renderer.SetPropertyBlock(block);
        }

        private static void ApplyIconLook(MeshRenderer renderer, Sprite icon, Color color)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetTexture(BaseMapProperty, icon.texture);
            block.SetColor(BaseColorProperty, color);
            renderer.SetPropertyBlock(block);
        }

        private void StopRoutine(ref Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
