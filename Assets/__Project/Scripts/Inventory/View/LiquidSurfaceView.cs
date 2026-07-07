using System.Collections;
using Inventory.Core;
using Inventory.Data.Definitions;
using UnityEngine;
using Zenject;

namespace Inventory.View
{
    /// <summary>
    /// The cauldron liquid (Track F): a full translucent top surface at the
    /// session's fill height, a cut-away curtain filling the pot's open wedge
    /// from floor to waterline (submerged bubbles read through it), and a
    /// bright thin waterline band at the meniscus. The top surface keeps the
    /// boil wave displacement and emission pulse. Pure presentation, no logic;
    /// the presenter drives only <see cref="SetFillHeight"/>.
    /// </summary>
    public class LiquidSurfaceView : MonoBehaviour, ILiquidSurfaceView
    {
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");

        private const float FullCircleRadians = 2f * Mathf.PI;
        private const float FullCircleDegrees = 360f;

        [Header("Surface Tessellation")]
        [SerializeField, Range(1, 16)] private int _rings = 6;
        [SerializeField, Range(3, 64)] private int _radialSegments = 16;

        [Header("Curtain & Waterline")]
        [Tooltip("How far the liquid sits inside the bowl's inner wall")]
        [SerializeField, Min(0f)] private float _wallInset = 0.005f;
        [SerializeField, Range(1, 12)] private int _curtainHeightSegments = 6;
        [SerializeField, Range(4, 64)] private int _curtainArcSegments = 24;
        [Tooltip("Vertical thickness of the bright waterline edge")]
        [SerializeField, Min(0.001f)] private float _waterlineThickness = 0.014f;
        [Tooltip("Material of the bright waterline band (the crisp meniscus edge)")]
        [SerializeField] private Material _waterlineMaterial;

        [Header("Scene References")]
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _renderer;
        [Tooltip("Owner of the bowl silhouette the liquid is shaped by")]
        [SerializeField] private CauldronView _cauldron;

        [Inject] private InventoryConfig _config;

        private Mesh _surfaceMesh;
        private Mesh _curtainMesh;
        private Mesh _waterlineMesh;
        private Vector3[] _baseVertices;
        private Vector3[] _animatedVertices;
        private Color _baseEmission;
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _boilCoroutine;
        private MeshFilter _curtainFilter;
        private MeshFilter _waterlineFilter;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();

            var material = _renderer != null ? _renderer.sharedMaterial : null;
            _baseEmission = material != null && material.HasProperty(EmissionColorProperty)
                ? material.GetColor(EmissionColorProperty)
                : Color.black;

            // The curtain and waterline live in bowl space (they span absolute
            // heights), so they parent to the cauldron, not to this surface
            // object whose Y is the moving fill height.
            _curtainFilter = CreateBandChild("LiquidCurtain", material);
            _waterlineFilter = CreateBandChild("Waterline", _waterlineMaterial);

            SetFillHeight(transform.localPosition.y);
        }

        public void SetFillHeight(float height)
        {
            if (_cauldron == null || _meshFilter == null)
            {
                return;
            }

            var bowl = _cauldron.ProfileSettings;
            height = Mathf.Clamp(height, bowl.WallThickness + 0.01f, bowl.BowlDepth);

            var position = transform.localPosition;
            position.y = height;
            transform.localPosition = position;

            float surfaceRadius = Mathf.Max(
                0.01f,
                CauldronProfileCalculator.InnerRadiusAtHeight(bowl, height) - _wallInset);
            _surfaceMesh = LiquidSurfaceMeshBuilder.Build(
                surfaceRadius, FullCircleDegrees, _rings, _radialSegments);
            _meshFilter.mesh = _surfaceMesh;
            _baseVertices = _surfaceMesh.vertices;
            _animatedVertices = new Vector3[_baseVertices.Length];

            // The pot wall sweeps ArcDegrees centred on +Z; the curtain fills
            // the remaining wedge so the cut-away reads as a body of liquid.
            float halfWall = _cauldron.ArcDegrees * 0.5f;
            float openStart = halfWall;
            float openEnd = FullCircleDegrees - halfWall;

            _curtainMesh = LiquidBandMeshBuilder.Build(
                bowl,
                bowl.WallThickness,
                height,
                openStart,
                openEnd,
                _curtainArcSegments,
                _curtainHeightSegments,
                -_wallInset,
                "LiquidCurtainMesh");
            _curtainFilter.mesh = _curtainMesh;

            _waterlineMesh = LiquidBandMeshBuilder.Build(
                bowl,
                height - _waterlineThickness,
                height,
                openStart,
                openEnd,
                _curtainArcSegments,
                1,
                -_wallInset * 0.5f,
                "WaterlineMesh");
            _waterlineFilter.mesh = _waterlineMesh;
        }

        private MeshFilter CreateBandChild(string childName, Material material)
        {
            var child = new GameObject(childName);
            child.layer = gameObject.layer;
            child.transform.SetParent(
                _cauldron != null ? _cauldron.transform : transform, false);

            var filter = child.AddComponent<MeshFilter>();
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return filter;
        }

        private void OnEnable()
        {
            _boilCoroutine = StartCoroutine(Boil());
        }

        private void OnDisable()
        {
            if (_boilCoroutine != null)
            {
                StopCoroutine(_boilCoroutine);
                _boilCoroutine = null;
            }
        }

        private IEnumerator Boil()
        {
            while (true)
            {
                if (_baseVertices != null)
                {
                    var settings = new LiquidWaveSettings(
                        _config != null ? _config.LiquidWaveAmplitude : 0f,
                        _config != null ? _config.LiquidWaveFrequency : 0f,
                        _config != null ? _config.LiquidWaveSpatialScale : 0f,
                        _config != null ? _config.LiquidSecondaryWaveWeight : 0f);

                    for (int i = 0; i < _baseVertices.Length; i++)
                    {
                        Vector3 vertex = _baseVertices[i];
                        vertex.y = LiquidWaveCalculator.SampleHeight(vertex.x, vertex.z, Time.time, settings);
                        _animatedVertices[i] = vertex;
                    }

                    _surfaceMesh.SetVertices(_animatedVertices);
                    _surfaceMesh.RecalculateNormals();

                    PulseEmission();
                }

                yield return null;
            }
        }

        private void PulseEmission()
        {
            if (_renderer == null || _config == null)
            {
                return;
            }

            float pulse = 1f + _config.LiquidEmissionPulseStrength
                * Mathf.Sin(Time.time * _config.LiquidEmissionPulseSpeed * FullCircleRadians);
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorProperty, _baseEmission * pulse);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
