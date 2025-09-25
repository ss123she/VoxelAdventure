using UnityEngine;
using UnityEngine.Serialization;

namespace Terrain
{
    [CreateAssetMenu(fileName = "New Terrain Settings", menuName = "Terrain/Terrain Settings")]
    public class TerrainSettings : ScriptableObject
    {
        [Header("Noise Settings")]
        public float terrainHeight = 4.0f;
        public float groundLevel = 2.0f;
        [Range(0f, 0.5f)]
        public float noiseScale = 0.04f;
        [Range(0f, 10f)]
        public int lacunarity = 1;
        [Range(1, 8)]
        public int octaves = 1;
        [Range(0, 1)]
        public float persistence = 0.5f;
        
        [Header("Cave noise settings")]
        [Range(0f, 1f)]
        public float caveDensity = 1.0f;
        
        [Header("General Settings")]
        public GenerationMode generationMode = GenerationMode.Caves;
    }
}