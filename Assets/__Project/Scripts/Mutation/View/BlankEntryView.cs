using System;
using System.Collections;
using System.Collections.Generic;
using Inventory.View;
using TMPro;
using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// One racked Part-Blank as a medallion (Track F): the part pictured at the
    /// centre on a species-tinted disc, its sockets set as gems evenly around
    /// the rim, the rim itself a progress ring that closes as sockets fill, and
    /// — once complete — a pulsing "Unseal" confirm the player clicks to open
    /// the variant menu. Visual construction only; events bubble up untouched.
    /// </summary>
    public class BlankEntryView : MonoBehaviour, IStageClickable
    {
        private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        [Header("Renderers")]
        [Tooltip("The blank's body quad; shows the icon when authored, else the species tint")]
        [SerializeField] private MeshRenderer _iconRenderer;

        [Header("Medallion")]
        [Tooltip("Radius of the rim progress ring; socket gems sit on it")]
        [SerializeField, Min(0.02f)] private float _rimRadius = 0.19f;
        [SerializeField, Min(0.002f)] private float _rimThickness = 0.02f;
        [SerializeField, Min(0.02f)] private float _discRadius = 0.155f;
        [Tooltip("Angular gap between rim segments, in degrees")]
        [SerializeField, Range(0f, 30f)] private float _rimSegmentGapDegrees = 10f;
        [SerializeField] private Color _discColor = new Color(0.13f, 0.12f, 0.11f, 1f);
        [SerializeField] private Color _emptyRimColor = new Color(0.22f, 0.21f, 0.2f, 1f);
        [Tooltip("Pulse speed of the closed (ready) rim, in pulses per second")]
        [SerializeField, Min(0f)] private float _readyPulseSpeed = 1.2f;

        [Header("Sockets")]
        [Tooltip("Disabled socket template cloned per socket; placed as gems on the rim")]
        [SerializeField] private SocketView _socketTemplate;

        [Header("Labels")]
        [SerializeField] private float _labelFontSize = 0.6f;

        private readonly List<SocketView> _sockets = new List<SocketView>();
        private readonly List<MeshRenderer> _rimSegments = new List<MeshRenderer>();
        private TextMeshPro _nameLabel;
        private TextMeshPro _unsealLabel;
        private MeshRenderer _discRenderer;
        private SphereCollider _unsealCollider;
        private Coroutine _readyPulseCoroutine;
        private int _blankInstanceId;
        private bool _isReady;
        private Color _speciesTint;

        /// <summary>Raised when a dragged artifact is dropped on any of this blank's sockets.</summary>
        public event Action<int /*artifactInstanceId*/, int /*blankInstanceId*/> OnArtifactDropped;

        /// <summary>Raised when one of this blank's filled sockets is clicked.</summary>
        public event Action<int /*blankInstanceId*/, int /*artifactInstanceId*/> OnFilledSocketClicked;

        /// <summary>Raised when the ready medallion is clicked (the unseal confirm).</summary>
        public event Action<int /*blankInstanceId*/> OnUnsealClicked;

        public void Configure(BlankEntryViewData data)
        {
            _blankInstanceId = data.BlankInstanceId;
            _isReady = data.IsReady;
            _speciesTint = data.SpeciesTint;

            EnsureDisc();
            ApplyIcon(data);
            EnsureNameLabel().text = data.DisplayName;
            RebuildRim(data);
            RebuildSockets(data);
            ApplyReadyState();
        }

        public void NotifyClicked()
        {
            if (_isReady)
            {
                OnUnsealClicked?.Invoke(_blankInstanceId);
            }
        }

        private void EnsureDisc()
        {
            if (_discRenderer != null)
            {
                return;
            }

            // Slightly behind the icon so the part reads on top of the disc.
            _discRenderer = CreateFlatPiece("MedallionDisc", new Vector3(0f, 0f, 0.01f));
            _discRenderer.GetComponent<MeshFilter>().mesh =
                MedallionMeshBuilder.BuildDisc(_discRadius, 28);
            TintRenderer(_discRenderer, _discColor);
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

        private void RebuildRim(BlankEntryViewData data)
        {
            foreach (var segment in _rimSegments)
            {
                if (segment != null)
                {
                    Destroy(segment.gameObject);
                }
            }

            _rimSegments.Clear();

            // One rim segment per socket: each closes its arc when its socket
            // fills, so the rim IS the progress line and the full ring reads
            // as the complete organ.
            float segmentSpan = 360f / data.SocketCount;
            float halfGap = _rimSegmentGapDegrees * 0.5f;
            for (int i = 0; i < data.SocketCount; i++)
            {
                float gemAngle = GemAngleDegrees(i, data.SocketCount);
                float start = gemAngle + halfGap;
                float end = gemAngle + segmentSpan - halfGap;
                var segment = CreateFlatPiece($"RimSegment{i}", Vector3.zero);
                segment.GetComponent<MeshFilter>().mesh = MedallionMeshBuilder.BuildRimArc(
                    _rimRadius - _rimThickness * 0.5f,
                    _rimRadius + _rimThickness * 0.5f,
                    start,
                    end,
                    10);
                bool filled = i < data.FilledSockets.Count;
                TintRenderer(segment, filled ? data.SpeciesTint : _emptyRimColor);
                _rimSegments.Add(segment);
            }
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

            for (int i = 0; i < data.SocketCount; i++)
            {
                var socket = Instantiate(_socketTemplate, transform);
                float angle = GemAngleDegrees(i, data.SocketCount) * Mathf.Deg2Rad;
                // Gems sit on the rim line itself, slightly proud of it.
                socket.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * _rimRadius,
                    Mathf.Sin(angle) * _rimRadius,
                    -0.005f);
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

        private void ApplyReadyState()
        {
            EnsureUnsealAffordance();

            _unsealCollider.enabled = _isReady;
            _unsealLabel.gameObject.SetActive(_isReady);

            if (_readyPulseCoroutine != null)
            {
                StopCoroutine(_readyPulseCoroutine);
                _readyPulseCoroutine = null;
            }

            if (_isReady && isActiveAndEnabled)
            {
                _readyPulseCoroutine = StartCoroutine(PulseReadyRim());
            }
        }

        private void EnsureUnsealAffordance()
        {
            if (_unsealCollider == null)
            {
                // The centre disc doubles as the confirm button; rim gems keep
                // their own colliders and win the raycast over this parent one.
                _unsealCollider = gameObject.AddComponent<SphereCollider>();
                _unsealCollider.radius = _discRadius;
            }

            if (_unsealLabel == null)
            {
                _unsealLabel = CreateLabel("UnsealLabel", new Vector3(0f, -(_rimRadius + 0.09f), 0f));
                _unsealLabel.text = "Unseal";
            }
        }

        private IEnumerator PulseReadyRim()
        {
            // The closed rim breathes toward white so "the organ is ready" reads
            // from across the ribbon; stopping restores the plain tint.
            while (true)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * _readyPulseSpeed * 2f * Mathf.PI);
                var color = Color.Lerp(_speciesTint, Color.white, pulse * 0.6f);
                foreach (var segment in _rimSegments)
                {
                    if (segment != null)
                    {
                        TintRenderer(segment, color);
                    }
                }

                yield return null;
            }
        }

        private MeshRenderer CreateFlatPiece(string pieceName, Vector3 localPosition)
        {
            var piece = new GameObject(pieceName);
            piece.layer = gameObject.layer;
            piece.transform.SetParent(transform, false);
            piece.transform.localPosition = localPosition;

            piece.AddComponent<MeshFilter>();
            var renderer = piece.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _iconRenderer != null ? _iconRenderer.sharedMaterial : null;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static void TintRenderer(MeshRenderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetTexture(BaseMapProperty, Texture2D.whiteTexture);
            block.SetColor(BaseColorProperty, color);
            renderer.SetPropertyBlock(block);
        }

        private static float GemAngleDegrees(int socketIndex, int socketCount)
        {
            // The first gem sits at the top; the rest spread evenly clockwise.
            return 90f - socketIndex * (360f / socketCount);
        }

        private TextMeshPro EnsureNameLabel()
        {
            if (_nameLabel == null)
            {
                _nameLabel = CreateLabel("Label", new Vector3(0f, _rimRadius + 0.09f, 0f));
            }

            return _nameLabel;
        }

        private TextMeshPro CreateLabel(string labelName, Vector3 localPosition)
        {
            // Plain 3D-TMP label built in code (the NpcOverheadView pattern) -
            // the default TMP font asset applies, no wiring needed.
            var labelObject = new GameObject(labelName);
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = localPosition;

            var label = labelObject.AddComponent<TextMeshPro>();
            label.fontSize = _labelFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(2f, 0.3f);
            return label;
        }

        private void OnDisable()
        {
            if (_readyPulseCoroutine != null)
            {
                StopCoroutine(_readyPulseCoroutine);
                _readyPulseCoroutine = null;
            }
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
