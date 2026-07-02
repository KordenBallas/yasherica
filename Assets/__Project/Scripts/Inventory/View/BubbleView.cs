using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// One floating artifact: a transparent bubble shell with a stack of lit,
    /// shadow-casting sprite quads inside. The rear layers sit slightly behind the
    /// front one and darken progressively, so the flat icon reads as a volume.
    /// Used both inside the pot and in the crafting slots above it.
    /// Clicks arrive from <see cref="StageDragRouter"/> via <see cref="NotifyClicked"/>.
    /// </summary>
    public class BubbleView : MonoBehaviour, IStageClickable
    {
        // URP Lit shader properties.
        private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        // Deterministic pseudo-random yaw per instance sells depth without Random state.
        private const float MaxYawDegrees = 15f;
        private const int YawHashStep = 137;

        [Header("Renderers")]
        [SerializeField] private MeshRenderer _shellRenderer;
        [SerializeField] private MeshRenderer _artifactQuadRenderer;
        [Tooltip("Shell opacity; the artifact tint alpha is ignored so the shell stays see-through")]
        [SerializeField, Range(0f, 1f)] private float _shellAlpha = 0.25f;

        [Header("Interaction")]
        [SerializeField] private Collider _clickCollider;

        private readonly List<MeshRenderer> _depthLayers = new List<MeshRenderer>();

        public int InstanceId { get; private set; }

        /// <summary>True while the drag router moves this bubble; the pot's drift skips it.</summary>
        public bool IsDragged { get; private set; }

        public event Action<int> OnClicked;

        public void SetDragged(bool dragged)
        {
            IsDragged = dragged;
        }

        public void Configure(int instanceId, Sprite icon, Color tint, in ArtifactDepthSettings depth)
        {
            InstanceId = instanceId;

            if (_artifactQuadRenderer != null && icon != null)
            {
                ApplyArtifactLook(_artifactQuadRenderer, icon, Color.white);

                float yaw = (instanceId * YawHashStep) % (int)(MaxYawDegrees * 2f) - MaxYawDegrees;
                _artifactQuadRenderer.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

                BuildDepthLayers(icon, depth);
            }

            if (_shellRenderer != null)
            {
                // Definitions author tints opaque; forcing the shell alpha keeps the
                // artifact visible inside instead of letting the tint cover it.
                var shellColor = new Color(tint.r, tint.g, tint.b, _shellAlpha);
                var block = new MaterialPropertyBlock();
                _shellRenderer.GetPropertyBlock(block);
                block.SetColor(BaseColorProperty, shellColor);
                _shellRenderer.SetPropertyBlock(block);
            }
        }

        public void SetShellVisible(bool visible)
        {
            if (_shellRenderer != null)
            {
                _shellRenderer.enabled = visible;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (_clickCollider != null)
            {
                _clickCollider.enabled = interactable;
            }
        }

        public void NotifyClicked()
        {
            OnClicked?.Invoke(InstanceId);
        }

        // Rear layers are clones of the front quad pushed back along its depth
        // axis, sharing the material so SRP batching keeps the stack cheap.
        private void BuildDepthLayers(Sprite icon, in ArtifactDepthSettings depth)
        {
            ClearDepthLayers();

            Transform front = _artifactQuadRenderer.transform;
            for (int i = 1; i < depth.LayerCount; i++)
            {
                var layer = Instantiate(_artifactQuadRenderer, front.parent);
                layer.transform.localRotation = front.localRotation;
                layer.transform.localScale = front.localScale;
                layer.transform.localPosition = front.localPosition
                    + front.localRotation * Vector3.forward * (depth.LayerSpacing * i);

                float fade = depth.LayerCount > 1 ? i / (float)(depth.LayerCount - 1) : 0f;
                float brightness = Mathf.Lerp(1f, depth.BackLayerDarkening, fade);
                ApplyArtifactLook(layer, icon, new Color(brightness, brightness, brightness, 1f));

                _depthLayers.Add(layer);
            }
        }

        private void ClearDepthLayers()
        {
            foreach (var layer in _depthLayers)
            {
                if (layer != null)
                {
                    Destroy(layer.gameObject);
                }
            }

            _depthLayers.Clear();
        }

        private static void ApplyArtifactLook(MeshRenderer renderer, Sprite icon, Color color)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetTexture(BaseMapProperty, icon.texture);
            block.SetColor(BaseColorProperty, color);
            renderer.SetPropertyBlock(block);
        }
    }
}
