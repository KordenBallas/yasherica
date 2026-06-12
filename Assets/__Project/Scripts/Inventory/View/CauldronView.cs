using Inventory.Core;
using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// Thin adapter that authors the cauldron geometry: maps its serialized size
    /// tunables to the profile calculator and assigns the generated low-poly
    /// cross-section mesh. Generated at runtime so tuning stays live in the inspector.
    /// </summary>
    public class CauldronView : MonoBehaviour
    {
        [Header("Silhouette")]
        [SerializeField, Min(0.05f)] private float _bowlRadius = 0.5f;
        [SerializeField, Min(0.05f)] private float _bowlDepth = 0.42f;
        [SerializeField, Min(0.005f)] private float _wallThickness = 0.05f;
        [SerializeField, Min(0f)] private float _rimWidth = 0.06f;
        [SerializeField, Range(3, 24)] private int _wallSegments = 6;

        [Header("Lathe")]
        [SerializeField, Range(3, 64)] private int _radialSegments = 12;
        [Tooltip("Swept wall angle; the missing wedge is the front cross-section opening")]
        [SerializeField, Range(90f, 360f)] private float _arcDegrees = 200f;

        [Header("Scene References")]
        [SerializeField] private MeshFilter _meshFilter;

        private void Awake()
        {
            var settings = new CauldronProfileSettings(
                _bowlRadius, _bowlDepth, _wallThickness, _rimWidth, _wallSegments);
            var profile = CauldronProfileCalculator.Calculate(settings);
            _meshFilter.mesh = CauldronMeshBuilder.Build(profile, _radialSegments, _arcDegrees);
        }
    }
}
