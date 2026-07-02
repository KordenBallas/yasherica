using System;
using Inventory.View;
using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// One artifact socket on a blank entry: a small translucent disc that shows
    /// the socketed artifact's icon when filled. Receives routed stage input as a
    /// drop target (drag release) and a clickable (unsocket gesture). No logic -
    /// it only reports; the presenter decides.
    /// </summary>
    public class SocketView : MonoBehaviour, IStageClickable, IArtifactDropTarget
    {
        private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        [Header("Renderers")]
        [Tooltip("The translucent socket disc, tinted by the blank's species colour")]
        [SerializeField] private MeshRenderer _ringRenderer;
        [Tooltip("The socketed artifact's icon quad; hidden while the socket is empty")]
        [SerializeField] private MeshRenderer _iconRenderer;
        [SerializeField, Range(0f, 1f)] private float _emptyRingAlpha = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _filledRingAlpha = 0.6f;

        private int _filledArtifactInstanceId = -1;

        /// <summary>Raised on click while filled, with the socketed artifact's instance id.</summary>
        public event Action<int> OnFilledClicked;

        /// <summary>Raised when a dragged artifact is released onto this socket.</summary>
        public event Action<int> OnArtifactDropped;

        public void ShowEmpty(Color speciesTint)
        {
            _filledArtifactInstanceId = -1;
            TintRing(speciesTint, _emptyRingAlpha);
            if (_iconRenderer != null)
            {
                _iconRenderer.enabled = false;
            }
        }

        public void ShowFilled(SocketViewData data, Color speciesTint)
        {
            _filledArtifactInstanceId = data.ArtifactInstanceId;
            TintRing(speciesTint, _filledRingAlpha);

            if (_iconRenderer != null)
            {
                _iconRenderer.enabled = true;
                var block = new MaterialPropertyBlock();
                _iconRenderer.GetPropertyBlock(block);
                if (data.Icon != null)
                {
                    block.SetTexture(BaseMapProperty, data.Icon.texture);
                }

                block.SetColor(BaseColorProperty, Color.white);
                _iconRenderer.SetPropertyBlock(block);
            }
        }

        public void NotifyClicked()
        {
            if (_filledArtifactInstanceId >= 0)
            {
                OnFilledClicked?.Invoke(_filledArtifactInstanceId);
            }
        }

        public void NotifyArtifactDropped(int artifactInstanceId)
        {
            OnArtifactDropped?.Invoke(artifactInstanceId);
        }

        private void TintRing(Color tint, float alpha)
        {
            if (_ringRenderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            _ringRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorProperty, new Color(tint.r, tint.g, tint.b, alpha));
            _ringRenderer.SetPropertyBlock(block);
        }
    }
}
