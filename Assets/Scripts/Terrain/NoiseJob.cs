using System;
using System.Runtime.CompilerServices;
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
        [ReadOnly] public float CaveDensity;

        public void Execute(int index)
        {
            const int size = NaiveSurfaceNets.Chunk.ChunkSize;
            var x = index / (size * size);
            var y = (index / size) % size;
            var z = index % size;
            
            var worldPos = new float3(x, y, z) + Offset;
            float finalSdf;

            switch (GenerationMode)
            {
                case GenerationMode.Landscape:
                {
                    var noiseVal = GetFractalNoise(worldPos, NoiseScale);
                    finalSdf = worldPos.y - GroundLevel - noiseVal * TerrainHeight;
                    break;
                }
                // Caves
                case GenerationMode.Caves:
                {
                    // 1. Surface part
                    var surfNoise = GetFractalNoise(worldPos, NoiseScale);
                    var surfaceSdf = GroundLevel - worldPos.y + surfNoise * TerrainHeight;

                    // 2. Cave part
                    var caveNoise = GetFractalNoise(worldPos, NoiseScale * 2.0f);
                    var caveSdf = CaveDensity - caveNoise;

                    finalSdf = math.min(surfaceSdf, caveSdf);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            finalSdf = math.clamp(finalSdf, -1.0f, 1.0f);
            DataNative[index] = (sbyte)(finalSdf * -127.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetFractalNoise(float3 pos, float frequency)
        {
            var noiseValue = 0.0f;
            var amplitude = 1.0f;

            for (var i = 0; i < Octaves; i++)
            {
                noiseValue += noise.snoise(pos * frequency) * amplitude;

                frequency *= Lacunarity;
                amplitude *= Persistence;
            }

            return noiseValue;
        }
    }
}