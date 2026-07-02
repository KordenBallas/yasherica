using UnityEngine;

namespace LevelGeneration
{
    /// <summary>
    /// Unity-side appearance carrier for the area generator. Everything shape/size/layout related
    /// moved to the authored <c>PlatformShapeConfig</c> SO (mapped to
    /// <c>LevelGeneration.Surface.PlatformShapeSettings</c>) with the platform-hex rework.
    /// </summary>
    public class AreaGeneratorConfig
    {
        public Material platformMaterial;
        public bool colorVariation = true;
    }
}
