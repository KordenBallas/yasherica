using System.Collections;
using System.Collections.Generic;
using Combat.Player;
using TMPro;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Plays one ghost at a time: a translucent clone of the caster on its cell facing the
    /// volley, a translucent clone of every displaced unit at its predicted destination,
    /// and a damage/heal label above every struck unit. Fade in → hold → fade out, then
    /// everything is destroyed. Unmistakably a preview — never the live board.
    /// </summary>
    public sealed class GhostPlaybackView : MonoBehaviour, IGhostPlaybackView
    {
        private ICombatUnitViewRegistry _registry;
        private Camera _camera;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<TMP_Text> _labels = new List<TMP_Text>();
        private Coroutine _playback;

        public void Initialize(ICombatUnitViewRegistry registry)
        {
            _registry = registry;
            _camera = Camera.main;
        }

        public void Play(GhostPlaybackPlan plan)
        {
            Stop();

            if (_registry == null || plan == null)
                return;

            if (_registry.TryGet(plan.CasterUnitId, out var casterVisual))
            {
                var casterGhost = GhostVisualCloner.Clone(casterVisual, transform, out var casterRenderers);
                casterGhost.transform.position = plan.CasterPosition;
                if (plan.CasterLookDirection != Vector3.zero)
                    casterGhost.transform.rotation = Quaternion.LookRotation(plan.CasterLookDirection);
                _spawned.Add(casterGhost);
                _renderers.AddRange(casterRenderers);
            }

            foreach (var marker in plan.Markers)
            {
                if (marker.IsDisplaced && _registry.TryGet(marker.UnitId, out var unitVisual))
                {
                    var destinationGhost = GhostVisualCloner.Clone(unitVisual, transform, out var unitRenderers);
                    destinationGhost.transform.position = marker.To;
                    destinationGhost.transform.rotation = unitVisual.rotation;
                    _spawned.Add(destinationGhost);
                    _renderers.AddRange(unitRenderers);
                }

                if (marker.Damage > 0 || marker.Heal > 0)
                    CreateNumberLabel(marker);
            }

            _playback = StartCoroutine(PlayOnceThenFade());
        }

        public void Stop()
        {
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }

            foreach (var spawned in _spawned)
            {
                if (spawned != null)
                    Destroy(spawned);
            }
            _spawned.Clear();
            _renderers.Clear();
            _labels.Clear();
        }

        private void LateUpdate()
        {
            if (_labels.Count == 0)
                return;

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            foreach (var label in _labels)
            {
                if (label != null)
                    label.transform.rotation = _camera.transform.rotation;
            }
        }

        private void CreateNumberLabel(GhostUnitMarker marker)
        {
            var labelGo = new GameObject($"GhostNumber_{marker.UnitId}");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.position = marker.From + Vector3.up * TelegraphStyle.DamageLabelHeight;

            var label = labelGo.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = TelegraphStyle.DamageLabelFontSize;
            label.enableWordWrapping = false;
            label.rectTransform.sizeDelta = new Vector2(2f, 1f);
            label.text = marker.Damage > 0 ? $"-{marker.Damage}" : $"+{marker.Heal}";
            label.color = marker.Damage > 0
                ? new Color(1f, 0.4f, 0.35f)
                : new Color(0.4f, 1f, 0.5f);

            _spawned.Add(labelGo);
            _labels.Add(label);
        }

        private IEnumerator PlayOnceThenFade()
        {
            yield return FadeTo(1f, TelegraphStyle.GhostFadeInSeconds);
            yield return new WaitForSeconds(TelegraphStyle.GhostHoldSeconds);
            yield return FadeTo(0f, TelegraphStyle.GhostFadeOutSeconds);

            _playback = null;
            Stop();
        }

        /// <summary>
        /// Fades ghost translucency (relative to TelegraphStyle.GhostAlpha) and label alpha.
        /// The ghost material is shared, so fading it fades every clone of this playback —
        /// only one ghost plays at a time by design.
        /// </summary>
        private IEnumerator FadeTo(float targetWeight, float duration)
        {
            float startWeight = targetWeight <= 0f ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                ApplyWeight(Mathf.Lerp(startWeight, targetWeight, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            ApplyWeight(targetWeight);
        }

        private void ApplyWeight(float weight)
        {
            foreach (var renderer in _renderers)
            {
                if (renderer == null || renderer.sharedMaterials.Length == 0)
                    continue;

                var material = renderer.sharedMaterial;
                var color = material.color;
                color.a = TelegraphStyle.GhostAlpha * weight;
                material.color = color;
                break; // Shared material: one write fades every ghost renderer.
            }

            foreach (var label in _labels)
            {
                if (label == null) continue;
                var color = label.color;
                color.a = weight;
                label.color = color;
            }
        }
    }
}
