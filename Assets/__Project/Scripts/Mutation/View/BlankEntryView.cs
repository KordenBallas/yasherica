using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// One racked Part-Blank on the stage: an icon quad tinted by the blank's
    /// species colour, a name label, and a row of artifact sockets below it,
    /// cloned from a disabled template to match the blank's socket count.
    /// Visual construction only - events bubble up to the rack view untouched.
    /// </summary>
    public class BlankEntryView : MonoBehaviour
    {
        private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        [Header("Renderers")]
        [Tooltip("The blank's body quad; shows the icon when authored, else the species tint")]
        [SerializeField] private MeshRenderer _iconRenderer;

        [Header("Sockets")]
        [Tooltip("Disabled socket template cloned per socket; positioned in a row below the icon")]
        [SerializeField] private SocketView _socketTemplate;
        [SerializeField] private float _socketSpacing = 0.16f;
        [SerializeField] private Vector3 _socketRowOffset = new Vector3(0f, -0.16f, 0f);

        [Header("Label")]
        [SerializeField] private float _labelOffsetY = 0.14f;
        [SerializeField] private float _labelFontSize = 0.6f;

        private readonly List<SocketView> _sockets = new List<SocketView>();
        private TextMeshPro _label;
        private int _blankInstanceId;

        /// <summary>Raised when a dragged artifact is dropped on any of this blank's sockets.</summary>
        public event Action<int /*artifactInstanceId*/, int /*blankInstanceId*/> OnArtifactDropped;

        /// <summary>Raised when one of this blank's filled sockets is clicked.</summary>
        public event Action<int /*blankInstanceId*/, int /*artifactInstanceId*/> OnFilledSocketClicked;

        public void Configure(BlankEntryViewData data)
        {
            _blankInstanceId = data.BlankInstanceId;

            ApplyIcon(data);
            EnsureLabel().text = data.DisplayName;
            RebuildSockets(data);
        }

        private void ApplyIcon(BlankEntryViewData data)
        {
            if (_iconRenderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            _iconRenderer.GetPropertyBlock(block);
            if (data.Icon != null)
            {
                block.SetTexture(BaseMapProperty, data.Icon.texture);
                block.SetColor(BaseColorProperty, Color.white);
            }
            else
            {
                // No authored icon yet: the species tint keeps the blank readable.
                block.SetColor(BaseColorProperty, data.SpeciesTint);
            }

            _iconRenderer.SetPropertyBlock(block);
        }

        private void RebuildSockets(BlankEntryViewData data)
        {
            foreach (var socket in _sockets)
            {
                if (socket != null)
                {
                    socket.OnArtifactDropped -= HandleArtifactDropped;
                    socket.OnFilledClicked -= HandleFilledSocketClicked;
                    Destroy(socket.gameObject);
                }
            }

            _sockets.Clear();

            if (_socketTemplate == null)
            {
                return;
            }

            float rowWidth = (data.SocketCount - 1) * _socketSpacing;
            for (int i = 0; i < data.SocketCount; i++)
            {
                var socket = Instantiate(_socketTemplate, transform);
                socket.transform.localPosition =
                    _socketRowOffset + new Vector3(i * _socketSpacing - rowWidth * 0.5f, 0f, 0f);
                socket.gameObject.SetActive(true);

                if (i < data.FilledSockets.Count)
                {
                    socket.ShowFilled(data.FilledSockets[i], data.SpeciesTint);
                }
                else
                {
                    socket.ShowEmpty(data.SpeciesTint);
                }

                socket.OnArtifactDropped += HandleArtifactDropped;
                socket.OnFilledClicked += HandleFilledSocketClicked;
                _sockets.Add(socket);
            }
        }

        private TextMeshPro EnsureLabel()
        {
            if (_label != null)
            {
                return _label;
            }

            // Plain 3D-TMP label built in code (the NpcOverheadView pattern) -
            // the default TMP font asset applies, no wiring needed.
            var labelObject = new GameObject("Label");
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, _labelOffsetY, 0f);

            _label = labelObject.AddComponent<TextMeshPro>();
            _label.fontSize = _labelFontSize;
            _label.alignment = TextAlignmentOptions.Center;
            _label.rectTransform.sizeDelta = new Vector2(2f, 0.3f);
            return _label;
        }

        private void HandleArtifactDropped(int artifactInstanceId)
        {
            OnArtifactDropped?.Invoke(artifactInstanceId, _blankInstanceId);
        }

        private void HandleFilledSocketClicked(int artifactInstanceId)
        {
            OnFilledSocketClicked?.Invoke(_blankInstanceId, artifactInstanceId);
        }
    }
}
