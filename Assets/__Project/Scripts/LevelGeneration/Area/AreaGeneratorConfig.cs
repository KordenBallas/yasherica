using UnityEngine;

namespace LevelGeneration
{
    public class AreaGeneratorConfig
    {
        [Header("Platform Shape")]
        public Vector2 platformSizeMin = new Vector2(3f, 2f);
        public Vector2 platformSizeMax = new Vector2(6f, 4f);
        public int edgeVertexCount = 10;
        public float edgeJitter = 0.25f;
        public float platformThickness = 1.0f;
        
        [Header("Platform Placement")]
        public float gapBetweenPlatforms = 2.0f;
        public float heightDeviation = 1.5f;
        
        [Header("Appearance")]
        public Material platformMaterial;
        public bool colorVariation = true;
    }
}

