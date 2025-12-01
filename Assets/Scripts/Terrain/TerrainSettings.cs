using UnityEngine;
using UnityEngine.Serialization;

namespace Terrain
{
    [CreateAssetMenu(fileName = "New Terrain Settings", menuName = "Terrain/Terrain Settings")]
    public class TerrainSettings : ScriptableObject
    {
        [Header("Noise Settings")]
        public float TerrainHeight = 4.0f;

        public float GroundLevel = 2.0f;

        [Range(0f, 0.5f)]
        public float NoiseScale = 0.04f;

        [Range(0f, 10f)]
        public float WarpStrength = 0f;
        
        [Range(0f, 10f)]
        public int Lacunarity = 1;

        [Range(1, 8)]
        public int Octaves = 1;

        [Range(0, 1)]
        public float Persistence = 0.5f;
        
        [Header("Cave noise settings")]
        [Range(0f, 1f)]
        public float CaveDensity = 1.0f;
        
        [Header("General Settings")]
        public bool RandomizeSeedOnPlay = true;
        public int WorldSeed = 1;
        public GenerationMode GenerationMode = GenerationMode.Landscape;
    }
}