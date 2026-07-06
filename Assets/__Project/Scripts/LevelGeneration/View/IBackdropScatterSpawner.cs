using UnityEngine;

namespace LevelGeneration
{
    /// <summary>
    /// Fills the distant-scatter horizon behind a forward span the layout just laid out
    /// (environment-dressing, Track E4). The seam keeps the area generator decoupled from the
    /// backdrop visuals, mirroring <see cref="IRouteLandmarkSpawner"/>: the generator hands over
    /// the half-open world-X span, the implementation owns kit resolution, planning, and meshes.
    /// </summary>
    public interface IBackdropScatterSpawner
    {
        /// <summary>Plans + instantiates the span's scatter under the area root (world-fixed).</summary>
        void Fill(float fromX, float toXExclusive, Transform parent);
    }
}
