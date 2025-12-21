using UnityEngine;

namespace LevelGeneration
{
    public class PerlinNoiseMap
    {
        private int seed;
        private float scale;
        private int octaves;
        
        public PerlinNoiseMap(int seed, float scale = 1f, int octaves = 4)
        {
            this.seed = seed;
            this.scale = scale;
            this.octaves = octaves;
        }
        
        public float GetHeight(float x, float z)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = scale;
            
            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (x + seed) * frequency;
                float sampleZ = (z + seed) * frequency;
                value += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
                
                amplitude *= 0.5f;
                frequency *= 2f;
            }
            
            return value;
        }
        
        public Vector3 GetPositionWithHeight(Vector2 position)
        {
            float height = GetHeight(position.x, position.y);
            return new Vector3(position.x, height, position.y);
        }
    }
}

