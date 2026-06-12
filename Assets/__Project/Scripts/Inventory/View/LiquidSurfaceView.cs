using System.Collections;
using Inventory.Core;
using Inventory.Data.Definitions;
using UnityEngine;
using Zenject;

namespace Inventory.View
{
    /// <summary>
    /// Animates the magical liquid inside the cauldron: displaces the generated
    /// surface mesh with the boil wave field and pulses the material emission so
    /// the liquid reads as continuously boiling. Pure presentation, no logic.
    /// </summary>
    public class LiquidSurfaceView : MonoBehaviour
    {
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");

        private const float FullCircleRadians = 2f * Mathf.PI;

        [Header("Surface Geometry")]
        [SerializeField, Min(0.01f)] private float _radius = 0.4f;
        [Tooltip("Must match the cauldron sweep so the straight edges line up with the cut planes")]
        [SerializeField, Range(90f, 360f)] private float _arcDegrees = 200f;
        [SerializeField, Range(1, 16)] private int _rings = 6;
        [SerializeField, Range(3, 64)] private int _radialSegments = 16;

        [Header("Scene References")]
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _renderer;

        [Inject] private InventoryConfig _config;

        private Mesh _mesh;
        private Vector3[] _baseVertices;
        private Vector3[] _animatedVertices;
        private Color _baseEmission;
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _boilCoroutine;

        private void Awake()
        {
            _mesh = LiquidSurfaceMeshBuilder.Build(_radius, _arcDegrees, _rings, _radialSegments);
            _meshFilter.mesh = _mesh;
            _baseVertices = _mesh.vertices;
            _animatedVertices = new Vector3[_baseVertices.Length];
            _propertyBlock = new MaterialPropertyBlock();

            var material = _renderer != null ? _renderer.sharedMaterial : null;
            _baseEmission = material != null && material.HasProperty(EmissionColorProperty)
                ? material.GetColor(EmissionColorProperty)
                : Color.black;
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

                _mesh.SetVertices(_animatedVertices);
                _mesh.RecalculateNormals();

                PulseEmission();

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
