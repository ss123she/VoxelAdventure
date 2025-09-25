using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Terrain
{
    [BurstCompile]
    public struct NoiseJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<sbyte> DataNative;

        [ReadOnly] public float NoiseScale;
        [ReadOnly] public float3 Offset;
        [ReadOnly] public float TerrainHeight;
        [ReadOnly] public float GroundLevel;

        [ReadOnly] public int Octaves;
        [ReadOnly] public float Lacunarity;
        [ReadOnly] public float Persistence;
        
        [ReadOnly] public GenerationMode GenerationMode;
        // For caves
        [ReadOnly] public float CaveDensity;

        public void Execute(int index)
        {
            const int size = NaiveSurfaceNets.Chunk.ChunkSize;
    
            var z = index % size;
            var y = (index / size) % size;
            var x = index / (size * size);

            var worldPos = new float3(x, y, z) + Offset;
            
            // 0 -> Caves
            // 1 -> Landscape
            switch (GenerationMode)
            {
                // Cave generation mode
                case GenerationMode.Caves:
                {
                    var surfaceFrequency = NoiseScale;
                    var surfaceAmplitude = 1.0f;
                    var surfaceNoiseValue = 0.0f;

                    for (var i = 0; i < Octaves; i++)
                    {
                        var simplexNoise = noise.snoise(worldPos * surfaceFrequency);
                        surfaceNoiseValue += simplexNoise * surfaceAmplitude;

                        surfaceFrequency *= Lacunarity;
                        surfaceAmplitude *= Persistence;
                    }

                    var caveFrequency = NoiseScale * 2.0f; 
                    var caveAmplitude = 1.0f;
                    var caveNoiseValue = 0.0f;

                    for (var i = 0; i < Octaves; i++)
                    {
                        var simplexNoise = noise.snoise(worldPos * caveFrequency);
                        caveNoiseValue += simplexNoise * caveAmplitude;
                        
                        caveFrequency *= Lacunarity;
                        caveAmplitude *= Persistence;
                    }
                    
                    
                    var surfaceSdf = GroundLevel - worldPos.y + surfaceNoiseValue * TerrainHeight;
                    
                    var caveSdf = CaveDensity - caveNoiseValue;

                    var finalSdf = math.min(surfaceSdf, caveSdf);
                    
                    finalSdf = math.clamp(finalSdf, -1.0f, 1.0f);
                    DataNative[index] = (sbyte)(finalSdf * -127.0f);
                    break;
                }
                
                // Landscape generation mode
                case GenerationMode.Landscape:
                {
                    var frequency = NoiseScale;
                    var amplitude = 1.0f;
                    var noiseValue = 0.0f;

                    for (var i = 0; i < Octaves; i++)
                    {
                        var simplexNoise = noise.snoise(worldPos * frequency);
                        noiseValue += simplexNoise * amplitude;

                        frequency *= Lacunarity;
                        amplitude *= Persistence;
                    }
            
                    var sdf = worldPos.y - GroundLevel - noiseValue * TerrainHeight;

                    sdf = math.clamp(sdf, -1.0f, 1.0f);
                    DataNative[index] = (sbyte)(sdf * -127.0f);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}